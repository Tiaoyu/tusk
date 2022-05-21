﻿using System;
using System.Collections.Generic;
using System.Text;

namespace tusk
{
    public class Singleton<T> where T : new()
    {
        private static T _instance = new T();
        public static T Instance { get { if (_instance == null) _instance = new T(); return _instance; } }
    }
}
