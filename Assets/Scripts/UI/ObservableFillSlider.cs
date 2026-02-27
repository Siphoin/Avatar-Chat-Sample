using UniRx;
using UnityEngine;
using System;

namespace AvatarChat.UI
{
    public class ObservableFillSlider : FillSlider
    {
        private IDisposable _valueSubscription;
        private IDisposable _maxSubscription;

        public void SubscribeTo(IObservable<float> valueObservable, IObservable<float> maxObservable = null)
        {
            _valueSubscription?.Dispose();
            _maxSubscription?.Dispose();

            if (maxObservable != null)
            {
                _maxSubscription = maxObservable
                    .Subscribe(max => MaxValue = max)
                    .AddTo(this);
            }

            _valueSubscription = valueObservable
                .Subscribe(val => SetValueSmoothly(val))
                .AddTo(this);
        }

        private void OnDestroy()
        {
            _valueSubscription?.Dispose();
            _maxSubscription?.Dispose();
        }
    }
}