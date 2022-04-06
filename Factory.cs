using System;
using LiveSplit.Model;
using LiveSplit.UI.Components;

[assembly: ComponentFactory(typeof(Factory))]

namespace LiveSplit.UI.Components
{
    public class Factory : IComponentFactory
    {
        public string ComponentName => "Scriptable Auto Splitter (High Precision)";
        public string Description => "Allows scripts written in the ASL language to define the splitting behavior.";
        public ComponentCategory Category => ComponentCategory.Control;
        public Version Version => Version.Parse("1.8.17");

        public string UpdateName => ComponentName;
        public string UpdateURL => "https://raw.githubusercontent.com/goofball564/LiveSplit.ScriptableAutoSplitterHighPrecision/master/";
        public string XMLURL => "Components/update.LiveSplit.ScriptableAutoSplitHighPrecision.xml";

        public IComponent Create(LiveSplitState state) => new ASLComponent(state);
        public IComponent Create(LiveSplitState state, string script) => new ASLComponent(state, script);
    }
}
