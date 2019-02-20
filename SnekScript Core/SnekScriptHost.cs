using IronPython.Runtime.Operations;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;


namespace Ardenfall.Snek
{

    internal class SnekScriptHost : ScriptHost
    {
        private readonly PlatformAdaptationLayer _layer = new SnekPlatformAdaptationLayer();
        public override PlatformAdaptationLayer PlatformAdaptationLayer
        {
            get { return _layer; }
        }
    }

}