using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ardenfall.Snek
{
    public class SnekScriptSource
    {
        private Mlf.MlfBlock block;
        private string expression;
        private ScriptSource scriptSource;

        public ScriptSource ScriptSource
        {
            get
            {
                if (scriptSource == null)
                {
                    if(block.path == null)
                        scriptSource = Snek.SnekScriptEngine.Instance.CreateScriptSourceFromString(expression);
                    else
                        scriptSource = Snek.SnekScriptEngine.Instance.CreateScriptSourceFromString(expression,block.path,SourceCodeKind.File);
                }

                return scriptSource;
            }
        }

        public SnekScriptSource(Mlf.MlfBlock block)
        {
            this.block = block;

            //Hook onto content change to reset script source
            block.OnContentChange += (content) =>
            {
                SetExpression(content);
            };

        }

        public void SetExpression(string expression)
        {
            if (block.enableFunctionWrapping)
                this.expression = "def _wrappedfunction():\n\t" + expression + "\n_wrappedfunction()";
            else
                this.expression = expression;

            if (scriptSource != null)
                scriptSource = null;
        }


    }


}