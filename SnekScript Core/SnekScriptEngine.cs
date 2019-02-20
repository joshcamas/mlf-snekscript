using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine;
using IronPython.Hosting;
using IronPython.Runtime.Operations;
using Ardenfall.Mlf;

namespace Ardenfall.Snek
{
    public class SnekScriptEngine
    {
        private static SnekScriptEngine instance;

        private ScriptEngine engine;
        private SnekScope defaultScope;

        //   private ScriptScope scope;

        //private Dictionary<string, object> globalVariables;
        private Dictionary<string, string> contextDefinitions;
        private Dictionary<string,SnekScope> contexts;

        public Action<string> OnLogError;
        public Action<string> OnLogWarning;
        public Action<string> OnLog;

        public static string propertyPrefix = "_p_";
        public static string argumentPrefix = "_a_";

        public ScriptEngine Engine
        {
            get { return engine; }
        }

        /*
        public ScriptScope Scope
        {
            get { return scope; }
        }*/


        [StaticDefine("ScriptEngine")]
        public static SnekScriptEngine Instance
        {
            get
            {
                if (instance == null)
                    instance = new SnekScriptEngine();
                return instance;
            }
        }

        #region Engine

        public SnekScriptEngine()
        {
            SetupEngine();
        }

        protected void SetupEngine()
        {

            var setup = Python.CreateRuntimeSetup(null);
            setup.HostType = typeof(SnekScriptHost);
            var runtime = new ScriptRuntime(setup);

            engine = runtime.GetEngineByTypeName(typeof(PythonContext).AssemblyQualifiedName);
            SnekImporter.OverrideImport(engine);

            defaultScope = new SnekScope(engine.CreateScope());

            SetupAssemblies();

            AddSearchPath(Application.streamingAssetsPath + "/");

            MlfProcessorManager.OnEngineInit(this);
        }

        protected void SetupAssemblies()
        {
            foreach(System.Reflection.Assembly assembly in TypeFinder.LoadedAssemblies)
            {
                engine.Runtime.LoadAssembly(assembly);
            }
        }

        protected void AddSearchPath(string path)
        {
            ICollection<string> searchPaths = engine.GetSearchPaths();
            searchPaths.Add(path);
            engine.SetSearchPaths(searchPaths);
        }

        #endregion

        #region Execution

        //Special snippet which automatically wraps the expression in a function and handles returning 
        public T ExecuteReturningSnippet<T>(string expression, MlfBlock owningBlock, SnekScope scope, params object[] arguments)
        {
            if (scope == null)
                scope = defaultScope;

            if (!expression.Contains("return"))
                expression = "return(" + expression + ")";

            //Replace newlines with tabbed newline
            expression = expression.Replace("\n", "\n\t");
            expression = expression.Replace("\r", "\r\t");

            //Wrap in function
            expression = "def _smartsnippet():\n\t" + expression + "\n_smartsnippetreturn = _smartsnippet()";

            scope.scriptScope.SetVariable("_smartsnippetreturn", default(T));

            ExecuteSnippet(expression, owningBlock, scope, arguments);

            T returnValue = scope.scriptScope.GetVariable<T>("_smartsnippetreturn");

            scope.scriptScope.RemoveVariable("_smartsnippetreturn");

            return returnValue;
        }

        //String snippet
        public object ExecuteSnippet(string expression,SnekScope scope)
        {
            if (scope == null)
                scope = defaultScope;

            return ExecuteSnippet(engine.CreateScriptSourceFromString(expression), null, scope);
        }

        //Execute a snippet owned by a block
        public object ExecuteSnippet(string expression, MlfBlock owningBlock, SnekScope scope, params object[] arguments)
        {
            if (scope == null)
                scope = defaultScope;

            if(owningBlock != null)
                return ExecuteSnippet(engine.CreateScriptSourceFromString(expression, owningBlock.path), owningBlock, scope, arguments);
            else
                return ExecuteSnippet(engine.CreateScriptSourceFromString(expression), owningBlock, scope, arguments);
        }

        //Execute a source snippet
        public object ExecuteSnippet(ScriptSource source, MlfBlock owningBlock, SnekScope scope, params object[] arguments)
        {
            string stream = null;
            Exception error = null;

            if (scope == null)
                scope = defaultScope;

            object output = ExecuteSnippet(source, owningBlock, scope, out stream, out error, arguments);

            if (stream != null)
            {
                string stackTrace = GetStackTrace(source, owningBlock);
                Log(stream, StackTraceLogType.None, stackTrace);
            }
                
            if (error != null)
            {
                string stackTrace = GetStackTrace(source, owningBlock, error);
                LogError(error.Message, StackTraceLogType.None, stackTrace);
            }
            
            return output;
        }

        //Execute source snippet with stream and error catching handling 
        public object ExecuteSnippet(ScriptSource source, MlfBlock owningBlock, SnekScope scope, out string stream, out Exception error, params object[] arguments)
        {
            stream = null;
            error = null;

            if (scope == null)
                scope = defaultScope;

            if (owningBlock != null)
            {
                ApplyPropertyList(scope, owningBlock.MlfProperties);
                ApplyArgumentList(owningBlock, scope, arguments);
                AddContextsToScope(owningBlock.contexts, scope, owningBlock, arguments);
            }
                

            object output = Execute(source, scope, out stream, out error);

            if (owningBlock != null)
            {
                RemoveContextsFromScope(owningBlock.contexts, scope, owningBlock);
                RemoveArgumentList(owningBlock, scope, arguments);
                RemovePropertyList(scope, owningBlock.MlfProperties);
            }
               
            return output;

        }

        //Execute a block
        public object Execute(MlfBlock block, SnekScope scope, Dictionary<string, MlfProperty> properties = null, params object[] arguments)
        {
            string stream = null;
            Exception error = null;

            if (scope == null)
                scope = defaultScope;

            //Find source
            SnekScriptSource source = (SnekScriptSource)block.GetFormatData("scriptSource");

            if (source == null)
                return null;

            ApplyPropertyList(scope,properties);
            ApplyArgumentList(block, scope, arguments);

            object output = Execute(block, scope,out stream, out error, arguments);

            RemoveArgumentList(block, scope, arguments);
            RemovePropertyList(scope,properties);

            if (stream != null)
            {
                string stackTrace = GetStackTrace(source.ScriptSource, block);
                Log(stream, StackTraceLogType.None, stackTrace);
            }

            if (error != null)
            {
                string stackTrace = GetStackTrace(source.ScriptSource, block, error);
                LogError(error.Message, StackTraceLogType.None, stackTrace);
            }

            return output;
        }

        //Execute block with stream and error catching data 
        public object Execute(MlfBlock block, SnekScope scope, out string stream, out Exception error, params object[] arguments)
        {
            stream = null;
            error = null;

            if (scope == null)
                scope = defaultScope;

            SnekScriptSource source = (SnekScriptSource)block.GetFormatData("scriptSource");
            
            if (source == null)
                return null;

            scope.scriptScope.SetVariable("block", block);

            AddContextsToScope(block.contexts, scope, block, arguments);

            object output = Execute(source.ScriptSource, scope, out stream, out error);

            RemoveContextsFromScope(block.contexts, scope, block);

            scope.scriptScope.RemoveVariable("block");

            return output;
            
        }

        //Execute with stream and error catching data 
        public object Execute(ScriptSource scriptSource, SnekScope scope, out string stream, out Exception error)
        {
            stream = null;
            error = null;

            if (scope == null)
                scope = defaultScope;

            //Temporarily disable stack trace
            StackTraceLogType defaultLogType = Application.GetStackTraceLogType(LogType.Log);
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

            StackTraceLogType defaultErrorType = Application.GetStackTraceLogType(LogType.Error);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);

            StackTraceLogType defaultWarningType = Application.GetStackTraceLogType(LogType.Warning);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);

            StackTraceLogType defaultExceptionType = Application.GetStackTraceLogType(LogType.Exception);
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.None);
            
            using (var memoryStream = new MemoryStream())
            {
                object output = null;

                engine.Runtime.IO.SetOutput(memoryStream, new StreamWriter(memoryStream));
                try
                {
                    output = scriptSource.ExecuteAndWrap(scope.scriptScope);

                }
                catch (Exception e)
                {
                    error = e;
                }
                finally
                {
                    //Reset stacktrace
                    Application.SetStackTraceLogType(LogType.Log, defaultLogType);
                    Application.SetStackTraceLogType(LogType.Error, defaultErrorType);
                    Application.SetStackTraceLogType(LogType.Exception, defaultExceptionType);
                    Application.SetStackTraceLogType(LogType.Warning, defaultWarningType);

                    //Output
                    var length = (int)memoryStream.Length;
                    var bytes = new byte[length];
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.Read(bytes, 0, length);
                    string outputstr = Encoding.UTF8.GetString(bytes, 0, length).Trim();

                    if (outputstr != null && outputstr != "")
                        stream = outputstr;
                }
                return output;
            }

        }


        #endregion

        #region Logging

        //Log, with optional stacktracetype
        public void Log(string log,StackTraceLogType type = StackTraceLogType.ScriptOnly, string stackTraceOverride = null)
        {
            StackTraceLogType defaultType = Application.GetStackTraceLogType(LogType.Log);
            Application.SetStackTraceLogType(LogType.Log, type);

            if (stackTraceOverride != null)
                Debug.Log("[Snek] " + log + "\n\n" + stackTraceOverride);
            else
                Debug.Log("[Snek] " + log);

            Application.SetStackTraceLogType(LogType.Log, defaultType);

            if (OnLog != null)
                OnLog(log);
        }

        //LogError, with optional stacktracetype
        public void LogError(string log, StackTraceLogType type = StackTraceLogType.ScriptOnly,string stackTraceOverride = null)
        {
            StackTraceLogType defaultType = Application.GetStackTraceLogType(LogType.Error);
            Application.SetStackTraceLogType(LogType.Error, type);

            if (stackTraceOverride != null)
                Debug.LogError("[Snek] " + log + "\n" + stackTraceOverride);
            else
                Debug.LogError("[Snek] " + log);
            
            Application.SetStackTraceLogType(LogType.Error, defaultType);

            if (OnLogError != null)
                OnLogError(log);
        }

        //LogError, with optional stacktracetype
        public void LogWarning(string log, StackTraceLogType type = StackTraceLogType.ScriptOnly, string stackTraceOverride = null)
        {
            StackTraceLogType defaultType = Application.GetStackTraceLogType(LogType.Warning);
            Application.SetStackTraceLogType(LogType.Warning, type);

            if (stackTraceOverride != null)
                Debug.LogWarning("[Snek] " + log + "\n\n" + stackTraceOverride);
            else
                Debug.LogWarning("[Snek] " + log);

            Application.SetStackTraceLogType(LogType.Warning, defaultType);

            if (OnLogWarning != null)
                OnLogWarning(log);
        }


        public static string GetStackTrace(ScriptSource source, MlfBlock block, Exception e = null)
        {
            string stackTrace = "";

            if (block == null)
                return null;

            //Print error
            if(e != null)
            {
                foreach (var frame in PythonOps.GetDynamicStackFrames(e))
                {
                    stackTrace += "\nCode: " + source.GetCodeLine(frame.GetFileLineNumber()) + "";
                    stackTrace += "\nLine: " + (frame.GetFileLineNumber() + block.line);
                }
            }
                
            //Block's MLF path
            if (block.path != null)
                stackTrace += "\nPath: " + block.path;

            return stackTrace;
        }

        #endregion

        #region Global Variables
        /*
        /// <summary>
        /// Clears all global variables in scope
        /// </summary>
        public void ResetGlobalVariables()
        {
            if (globalVariables != null && scope != null)
            {
                foreach (KeyValuePair<string, object> pair in globalVariables)
                {
                    scope.RemoveVariable(pair.Key);
                }
            }

            globalVariables = new Dictionary<string, object>();
        }

        /// <summary>
        /// Set a global variable for scope
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public void AddGlobalVariable(string id, object value)
        {
            if (scope == null)
                return;

            if (globalVariables == null)
                globalVariables = new Dictionary<string, object>();

            globalVariables[id] = value;

            scope.SetVariable(id, value);
        }

        /// <summary>
        /// Remove a global variable for scope
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public void RemoveGlobalVariable(string id)
        {
            if (scope == null)
                return;

            if (globalVariables == null)
                return;

            globalVariables.Remove(id);
            scope.RemoveVariable(id);

        }

        public object GetGlobalVariable(string id)
        {
            if (scope == null)
                return null;

            return scope.GetVariable(id);
        }
        */

        private void ApplyArgumentList(MlfBlock block,SnekScope scope,object[] arguments)
        {
            if (arguments == null)
                return;

            for(int i =0;i<arguments.Length;i++)
            {
                if (block.arguments.Count <= i)
                    continue;

                scope.scriptScope.SetVariable(block.arguments[i], arguments[i]);
            }
        }

        private void RemoveArgumentList(MlfBlock block, SnekScope scope, object[] arguments)
        {
            if (arguments == null)
                return;

            for (int i = 0; i < arguments.Length; i++)
            {
                if (block.arguments.Count <= i)
                    continue;

                scope.scriptScope.RemoveVariable(block.arguments[i]);
            }
        }

        private void ApplyPropertyList(SnekScope scope, Dictionary<string, MlfProperty> properties)
        {
            if (properties == null)
                return;

            foreach (KeyValuePair<string, MlfProperty> pair in properties)
            {
                scope.scriptScope.SetVariable(propertyPrefix + pair.Key, pair.Value.Value);
            }
        }

        private void RemovePropertyList(SnekScope scope, Dictionary<string, MlfProperty> properties)
        {
            if (properties == null)
                return;

            foreach (KeyValuePair<string, MlfProperty> pair in properties)
            {
                scope.scriptScope.RemoveVariable(propertyPrefix + pair.Key);
            }
        }

        #endregion

        #region Utility
        
        public void ResetRuntime()
        {
            contexts = new Dictionary<string, SnekScope>();
            contextDefinitions = new Dictionary<string, string>();

        }

        //Executes a python formatted block
        public static object ExecuteMlfBlock(MlfObject mlfObject,SnekScope scope,string id = null, string tag = null, params object[] arguments)
        {
            mlfObject.MlfInstance.InterpretIfDirty();

            MlfBlock block = mlfObject.MlfInstance.GetBlock(id, tag, "python");

            if(block != null)
            {
                return Instance.Execute(block, scope, mlfObject.MlfProperties, arguments);
            }
            return null;
        }

        public ScriptSource CreateScriptSourceFromString(string expression)
        {
            return engine.CreateScriptSourceFromString(expression);
        }

        public ScriptSource CreateScriptSourceFromString(string expression,string path,Microsoft.Scripting.SourceCodeKind kind)
        {
            return engine.CreateScriptSourceFromString(expression, path, kind);
        }

        //Runs a script without being tied to an instance or engine (only use for debugging)
        public static object QuickExecute(string expression)
        {
            if (!Application.isEditor)
                Debug.LogError("Do not use 'QuickExecute' in Application: Use SnekScriptEngine instance");

            SnekScriptEngine engine = new SnekScriptEngine();

            return engine.ExecuteSnippet(expression, new SnekScope(engine.Engine.CreateScope()));
        }

        //Compiles a script without being tied to an instance or engine (only use for debugging)
        public static object QuickCompile(string expression)
        {
            if (!Application.isEditor)
                Debug.LogError("Do not use 'QuickCompile' in Application: Use SnekScriptEngine instance");

            try
            {
                SnekScriptEngine engine = new SnekScriptEngine();
                ScriptSource source = engine.CreateScriptSourceFromString(expression);
                CompiledCode command = source.Compile();
                return command;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        #endregion

        public void AddContextsToScope(List<MlfContextReference> contexts, SnekScope scope=null, MlfBlock block=null, params object[] arguments)
        {
            if (contexts == null)
                return;

            if (scope == null)
                scope = defaultScope;

            foreach (MlfContextReference context in contexts)
            {
                ScriptScope contextScope = GetContext(context.id).scriptScope;

                //Add local arguments
                if (block != null)
                {
                    for (int i = 0; i < block.arguments.Count; i++)
                    {
                        if (arguments.Length <= i)
                            break;

                        contextScope.SetVariable(block.arguments[i], arguments[i]);
                    }
                }
                //Wildcard mode (Not recommended)
                if (context.key == "*")
                {
                    //Detect adding wildcard to default scope, which is currently not allowed
                    if(scope == defaultScope)
                    {
                        Debug.LogError("Warning: Adding Wildcard Contexts to default scope is not allowed, since currently wildcard contexts cannot be removed.");
                        continue;
                    }
                        
                    foreach (string var in contextScope.GetVariableNames())
                    {
                        object value = contextScope.GetVariable(var);

                        if(value != null)
                            scope.scriptScope.SetVariable(var, value);
                    }
                } else
                {
                    scope.scriptScope.SetVariable(context.key, contextScope);
                }
            }
        }

        public void RemoveContextsFromScope(List<MlfContextReference> contexts, SnekScope scope = null, MlfBlock block=null)
        {
            if (contexts == null)
                return;

            if (scope == null)
                scope = defaultScope;

            foreach (MlfContextReference context in contexts)
            {
                ScriptScope contextScope = GetContext(context.id).scriptScope;

                //Remove local arguments
                if (block != null)
                {
                    for (int i = 0; i < block.arguments.Count; i++)
                    {
                        contextScope.RemoveVariable(block.arguments[i]);
                    }
                }

                //Wildcard mode (Not recommended)
                if (context.key == "*")
                {
                    //Currently wildcard mode cannot remove variables, since it would include crazy variables such as "False"
                    //Possible (yucky) solution: when a context is defined, add the variable to a dictionary

                } else
                {
                    scope.scriptScope.RemoveVariable(context.key);
                }
            }
        }

        //Returns a cached context scope
        private SnekScope GetContext(string id)
        {
            if (contextDefinitions == null)
                return null;

            if (contexts == null)
                contexts = new Dictionary<string, SnekScope>();

            //Generate missing context if definition exists
            if (!contexts.ContainsKey(id) && contextDefinitions.ContainsKey(id))
                contexts.Add(id,BuildContext(id));

            return contexts[id];
        }

        public SnekScope BuildContext(string id)
        {
            SnekScope context = new SnekScope(Engine.CreateScope());
            
            //Generate context using definition
            if(contextDefinitions != null && contextDefinitions.ContainsKey(id))
            {
                ExecuteSnippet(contextDefinitions[id], context);
            }

            return context;
        }
        
        public void AddContextDefinition(string id, string expression)
        {
            if (contextDefinitions == null)
                contextDefinitions = new Dictionary<string, string>();

            //Add definition to the bottom of the existing expression
            if(contextDefinitions.ContainsKey(id))
                contextDefinitions[id] += "\n" + expression;
            //Set expression
            else
                contextDefinitions[id] = expression;
        }
        /*
        #endregion
        
        #region Scope

        //Helper function that builds a scope by the name of "local"
        public SnekScope BuildLocalScope(MlfInstance instance)
        {
            return BuildScope(instance, "local");
        }

        public SnekScope BuildScope(MlfInstance instance,string scopeName)
        {
            List<MlfBlock> blocks = instance.GetBlocks("DefineLocalScope");

            if (blocks.Count == 0)
                return null;

            SnekScope snekScope = new SnekScope(scopeName);

            AddGlobalVariable(scopeName, snekScope.values);

            foreach (MlfBlock block in blocks)
            {
                Execute(block);
            }

            snekScope.values = (Dictionary<string,object>)GetGlobalVariable(scopeName);
            RemoveGlobalVariable(scopeName);

            return snekScope;
        }

        private void StartSnekScope(SnekScope snekScope)
        {
            if(scope != null)
            {
                scope.SetVariable(snekScope.name, snekScope.values);
            }
        }

        private void EndSnekScope(SnekScope snekScope)
        {
            scope.RemoveVariable(snekScope.name);
        }


        #endregion
    */
    }

}
