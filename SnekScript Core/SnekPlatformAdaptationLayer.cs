using System.IO;
using Microsoft.Scripting;
using UnityEngine;
using Ardenfall.Mlf;

namespace Ardenfall.Snek
{
    internal class SnekPlatformAdaptationLayer : PlatformAdaptationLayer
    {
        public override Stream OpenOutputFileStream(string path)
        {
            return base.OpenInputFileStream(path);
        }

        public override Stream OpenInputFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
        {
            return base.OpenInputFileStream(path, mode, access, share, bufferSize);
        }
    }
}