using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using Ardenfall.Snek;

namespace Ardenfall.Mlf
{
    //Adds the format: "python".
    public class PythonFormat : MlfProcessor
    {
        public override void OnMlfFormatPre(MlfInstance instance)
        {
            foreach (MlfBlock block in instance.Blocks)
            {
                if (block.format != "python")
                    continue;

                SnekScriptSource source = new SnekScriptSource(block);

                block.SetFormatData("scriptSource", source);


            }
        }

    }

}