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
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class MultifactorProgressTracker : IEnumerable {
        private readonly List<ProgressFactor> _factors = new List<ProgressFactor>();
        private int _total;
        public int Progress { get; private set; }

        public delegate void Changed(int progress);

        public event Changed ProgressChanged;

        private void RecalcTotal() {
            _total = _factors.Sum(each => each.Weight*100);
            Updated();
        }

        public void Updated() {
            var progress = _factors.Sum(each => each.Weight*each.Progress);
            progress = (progress*100/_total);

            if (Progress != progress) {
                Progress = progress;
                if (ProgressChanged != null) {
                    ProgressChanged(Progress);
                }
            }
        }

        public static implicit operator int(MultifactorProgressTracker progressTracker) {
            return progressTracker.Progress;
        }

        public void Add(ProgressFactor factor) {
            _factors.Add(factor);
            factor.Tracker = this;
            RecalcTotal();
        }

        public IEnumerator GetEnumerator() {
            throw new NotImplementedException();
        }
    }
}