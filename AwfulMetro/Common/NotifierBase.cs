﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AwfulMetro.Annotations;

namespace AwfulMetro.Common
{
    public abstract class NotifierBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected virtual void RaiseEvent<TArgs>(EventHandler<TArgs> handler, TArgs args)
        {
            if (handler != null)
            {
                handler(this, args);
            }
        }
    }
}