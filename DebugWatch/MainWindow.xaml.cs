﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DebugWatch {
    using CoApp.DebugWatch;
    using CoApp.Developer.Toolkit.Debugging;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public DebugMessages DebugMessages { get; set; }

        public MainWindow() {
            DebugMessages = new DebugMessages();
            InitializeComponent();
            itemsGrid.CanUserAddRows = false;
            itemsGrid.CanUserResizeRows = false;

            DebugMessages.CollectionChanged += (sender, args) => {
                itemsGrid.ScrollIntoView(DebugMessages.Last());
            };
            Monitor.Start();
        }

        
    }
}