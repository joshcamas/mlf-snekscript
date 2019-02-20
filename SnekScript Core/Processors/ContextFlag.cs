using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ardenfall.Mlf
{
    //{{Context "Dialog"("d")}}
    public class ContextFlag : MlfProcessor
    {
        public override string DebugInstance(MlfInstance instance)
        {
            List<MlfFlag> flags = instance.GetFlags("Context");

            if (flags.Count == 0)
                return null;

            string output = "Context Flags:\n";

            foreach (MlfFlag flag in flags)
            {
                //Bad Context flag
                if (flag.tags.Count != 1)
                    output += "(Bad tag)";

                if (flag.arguments.Count != 1)
                    output += "(Bad argument)";
                else
                    output += "ID: " + flag.tags[0] + ", Key: " + flag.arguments[0];

            }

            return output;

        }

        public override void OnMlfInstanceInterpret(MlfInstance instance)
        {
            List<MlfFlag> flags = instance.GetFlags("Context");

            if (flags.Count == 0)
                return;
            
            foreach (MlfBlock block in instance.Blocks)
            {
                block.contexts = new List<MlfContextReference>();
                
                foreach(MlfFlag flag in flags)
                {
                    //Bad Context flag
                    if (flag.tags.Count != 1)
                        continue;

                    if (flag.arguments.Count != 1)
                        continue;
                    
                    block.contexts.Add(new MlfContextReference(flag.tags[0], flag.arguments[0]));
                }
            }

        }
    }

}