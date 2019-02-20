using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ardenfall.Mlf
{
    //[[Context("id1","id2")]
    //Adds context definitions to the Snek Script Engine

    public class ContextBlock : MlfProcessor
    {
        public override void OnMlfFormatPost(MlfInstance instance)
        {
            List<MlfBlock> blocks = instance.GetBlocks("DefineContext");

            foreach(MlfBlock block in blocks)
            {
                if(block.format != "python")
                {
                    Debug.LogError("Context blocks must be in python format");
                    continue;
                }

                if(block.tags.Count == 0)
                {
                    Debug.LogError("Context block must have at least one tag, defining what context it is defining");
                    continue;
                }

                block.enableFunctionWrapping = false;

                foreach (string tag in block.tags)
                {
                    Snek.SnekScriptEngine.Instance.AddContextDefinition(tag, block.Content);
                }
            }

        }
    }

}