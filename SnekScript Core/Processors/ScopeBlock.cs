using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ardenfall.Mlf
{
    //Defines a loca scope for a script
    public class ScopeBlock : MlfProcessor
    {
        public override void OnMlfFormatPost(MlfInstance instance)
        {
            List<MlfBlock> blocks = instance.GetBlocks("DefineLocalScope");

            foreach (MlfBlock block in blocks)
            {
                if (block.format != "python")
                {
                    Debug.LogError("Scope blocks must be in python format");
                    continue;
                }

            }

        }
    }

}