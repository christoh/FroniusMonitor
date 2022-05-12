﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using De.Hochstaetter.FroniusMonitor.ViewModels;

namespace De.Hochstaetter.FroniusMonitor.Views
{
    /// <summary>
    /// Interaction logic for SelfConsumptionOptimizationView.xaml
    /// </summary>
    public partial class SelfConsumptionOptimizationView
    {
        public SelfConsumptionOptimizationView(SelfConsumptionOptimizationViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            Loaded += async (s, e) =>
            {
                ViewModel.Dispatcher = Dispatcher;
                await ViewModel.OnInitialize().ConfigureAwait(false);
            };
        }

        public SelfConsumptionOptimizationViewModel ViewModel => (SelfConsumptionOptimizationViewModel)DataContext;
    }
}