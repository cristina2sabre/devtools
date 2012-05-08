//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010-2012 Garrett Serack and CoApp Contributors. 
//     Contributors can be discovered using the 'git log' command.
//     All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Bootstrapper {
    public class ProgressFactor {
        internal int Weight;
        private int _progress;

        public int Progress {
            get {
                return _progress;
            }
            set {
                if (value >= 0 && value <= 100 && _progress != value) {
                    _progress = value;
                    Tracker.Updated();
                }
            }
        }

        public ProgressFactor(ProgressWeight weight) {
            Weight = (int)weight;
        }

        internal MultifactorProgressTracker Tracker;
    }
}