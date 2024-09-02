// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fantasy
{
    public sealed class MessagePoolComponent : Entity
    {
        private int _poolCount;
        private const int MaxCapacity = ushort.MaxValue;
        private readonly OneToManyQueue<Type, AMessage> _poolQueue = new OneToManyQueue<Type, AMessage>();
        private readonly Dictionary<Type, Func<AMessage>> _typeCheckCache = new Dictionary<Type, Func<AMessage>>();

        public override void Dispose()
        {
            _poolCount = 0;
            _poolQueue.Clear();
            _typeCheckCache.Clear();
            base.Dispose();
        }

        public T Rent<T>() where T : AMessage, new()
        {
            if (!_poolQueue.TryDequeue(typeof(T), out var queue))
            {
                var instance = new T();
                instance.Scene = Scene;
                instance.IsPool = true;
                return instance;
            }

            queue.IsPool = true;
            _poolCount--;
            return (T)queue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AMessage Rent(Type type)
        {
            if (!_poolQueue.TryDequeue(type, out var queue))
            {
                if (!_typeCheckCache.TryGetValue(type, out var createInstance))
                {
                    if (!typeof(AMessage).IsAssignableFrom(type))
                    {
                        throw new NotSupportedException($"{this.GetType().FullName} Type:{type.FullName} must inherit from IPool");
                    }
                    else
                    {
                        createInstance = CreateInstance.CreateMessage(type);
                        _typeCheckCache[type] = createInstance;
                    }
                }
                
                var instance = createInstance();
                instance.Scene = Scene;
                instance.IsPool = true;
                return instance;
            }
            
            queue.IsPool = true;
            _poolCount--;
            return queue;
        }

        public void Return(AMessage obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!obj.IsPool)
            {
                return;
            }

            if (_poolCount >= MaxCapacity)
            {
                return;
            }
            
            _poolCount++;
            obj.IsPool = false;
            _poolQueue.Enqueue(obj.GetType(), obj);
        }

        public void Return<T>(T obj) where T : AMessage
        {
            if (obj == null)
            {
                return;
            }

            if (!obj.IsPool)
            {
                return;
            }

            if (_poolCount >= MaxCapacity)
            {
                return;
            }
            
            _poolCount++;
            obj.IsPool = false;
            _poolQueue.Enqueue(typeof(T), obj);
        }
    }
}