using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ardenfall.Mlf
{
    //Adds flag: DefaultImport
    //Automatically imports UnityEngine as u for python formatted blocks
    //Usage: {{DefaultImport}}
    public class DefaultImportFlag : MlfProcessor
    {
        public override void OnMlfInstanceInterpret(MlfInstance instance)
        {
            MlfFlag flag = instance.GetFlag("DefaultImport");

            if(flag != null)
            {
                foreach (MlfBlock block in instance.Blocks)
                {
                    if(block.format == "python")
                        block.AddPrefixText("import UnityEngine as u");
                }
            }
        }
    }

}