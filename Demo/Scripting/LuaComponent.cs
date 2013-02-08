using System;
using System.Collections.Generic;
using System.IO;
using Calcifer.Engine.Components;
using Calcifer.Engine.Scenery;
using Calcifer.Utilities;
using ComponentKit.Model;
using Demo.Components;
using LuaInterface;

namespace Demo.Scripting
{
    public class LuaComponent : Component, IUpdateable, ISaveable, IConstructable
    {
        private float wait;

        public LuaService Service { get; set; }
        public string Source { get; private set; }
        public bool UseDeprecatedAPI { get; set; }

        public bool IsWaiting
        {
            get { return wait > 0f; }
        }

        public void Update(double dt)
        {
            if (IsWaiting) wait -= (float) dt;
            else if (UseDeprecatedAPI) 
                Service.ExecuteScript(this);
        }

        public void Wait(float seconds)
        {
            wait = seconds;
        }

        public void SaveState(BinaryWriter writer)
        {
            writer.Write(wait);
        }

        public void RestoreState(BinaryReader reader)
        {
            wait = reader.ReadSingle();
        }

        void IConstructable.Construct(IDictionary<string, string> param)
        {
            if (param.Get("source", null) != null)
                Source = param.Get("source", null);
            else
                Source = File.Exists(param["sourceRef"]) ? File.ReadAllText(param["sourceRef"]) : "";
        }
    }
}