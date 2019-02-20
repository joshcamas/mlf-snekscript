using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using IronPython.Hosting;
using Ardenfall.Mlf;

namespace Ardenfall.Snek
{
    public class SnekImporter
    {
        delegate object ImportDelegate(CodeContext context, string moduleName, PythonDictionary globals, PythonDictionary locals, PythonTuple tuple);

        public static void OverrideImport(ScriptEngine engine)
        {
            ScriptScope scope = IronPython.Hosting.Python.GetBuiltinModule(engine);

            scope.SetVariable("__import__", new ImportDelegate(DoDatabaseImport));
        }

        protected static object DoDatabaseImport(CodeContext context, string moduleName, PythonDictionary globals, PythonDictionary locals, PythonTuple tuple)
        {
            /*
            if (ScriptExistsInDb(moduleName))
            {
                string rawScript = GetScriptFromDb(moduleName);
                ScriptSource source = Engine.CreateScriptSourceFromString(rawScript);
                ScriptScope scope = Engine.CreateScope();
                Engine.Execute(rawScript, scope);
                Microsoft.Scripting.Runtime.Scope ret = Microsoft.Scripting.Hosting.Providers.HostingHelpers.GetScope(scope);
                Scope.SetVariable(moduleName, ret);
                return ret;
            }
            */

            // fall back on the built-in method
            return IronPython.Modules.Builtin.__import__(context, moduleName);
        }
    }
}