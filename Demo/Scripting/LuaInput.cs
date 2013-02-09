using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LuaInterface;
using OpenTK.Input;

namespace Demo.Scripting
{
    class LuaInput
    {
        private KeyboardState state, oldState;
        public Table Keys { get; private set; }
        private readonly Dictionary<Key, LuaFunction> triggers = new Dictionary<Key, LuaFunction>();

        public LuaInput()
        {
            oldState = Keyboard.GetState();
            Keys = new Table();
            foreach (var key in Enum.GetNames(typeof (Key)))
                Keys[key] = Enum.Parse(typeof(Key), key);
        }

        public void Bind(Key key, LuaFunction func)
        {
            if (triggers.ContainsKey(key)) triggers[key] = func;
            else triggers.Add(key, func);
        }

        public void Poll()
        {
            oldState = state;
            state = Keyboard.GetState();
            foreach (var trigger in triggers.Where(trigger => oldState[trigger.Key] != state[trigger.Key]))
            {
                trigger.Value.Call(state[trigger.Key]);
            }
        }
    }

    class Table
    {
        private Hashtable table = new Hashtable();

        public object this[object key] 
        {
            get 
            {
                return table.Contains(key) ? table[key] : null;
            }
            set
            {
                if (table.Contains(key)) table[key] = value;
                else table.Add(key, value);
            }
        }
    }
}
