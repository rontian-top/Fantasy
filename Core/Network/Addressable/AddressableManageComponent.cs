#if FANTASY_NET
using System;
using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
    public class AddressableManageComponentAwakeSystem : AwakeSystem<AddressableManageComponent>
    {
        protected override void Awake(AddressableManageComponent self)
        {
            self.AddressableLock = self.Scene.CoroutineLockComponent.Create(self.GetType().TypeHandle.Value.ToInt64());
        }
    }
    
    public class AddressableManageComponentDestroySystem : DestroySystem<AddressableManageComponent>
    {
        protected override void Destroy(AddressableManageComponent self)
        {
            foreach (var (_, waitCoroutineLock) in self.Locks)
            {
                waitCoroutineLock.Dispose();
            }
            
            self.Locks.Clear();
            self.Addressable.Clear();
            self.AddressableLock.Dispose();
            self.AddressableLock = null;
        }
    }

    public sealed class AddressableManageComponent : Entity
    {
        public CoroutineLockQueueType AddressableLock;
        public readonly Dictionary<long, long> Addressable = new();
        public readonly Dictionary<long, WaitCoroutineLock> Locks = new();
        
        /// <summary>
        /// 添加地址映射。
        /// </summary>
        /// <param name="addressableId">地址映射的唯一标识。</param>
        /// <param name="routeId">路由 ID。</param>
        /// <param name="isLock">是否进行锁定。</param>
        public async FTask Add(long addressableId, long routeId, bool isLock)
        {
            WaitCoroutineLock waitCoroutineLock = null;
            
            try
            {
                if (isLock)
                {
                    waitCoroutineLock = await AddressableLock.Lock(addressableId);
                }
                
                Addressable[addressableId] = routeId;
                Log.Debug($"AddressableManageComponent Add addressableId:{addressableId} routeId:{routeId}");
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                waitCoroutineLock?.Dispose();
            }
        }

        /// <summary>
        /// 获取地址映射的路由 ID。
        /// </summary>
        /// <param name="addressableId">地址映射的唯一标识。</param>
        /// <returns>地址映射的路由 ID。</returns>
        public async FTask<long> Get(long addressableId)
        {
            using (await AddressableLock.Lock(addressableId))
            {
                Addressable.TryGetValue(addressableId, out var routeId);
                return routeId;
            }
        }

        /// <summary>
        /// 移除地址映射。
        /// </summary>
        /// <param name="addressableId">地址映射的唯一标识。</param>
        public async FTask Remove(long addressableId)
        {
            using (await AddressableLock.Lock(addressableId))
            {
                Addressable.Remove(addressableId);
                Log.Debug($"Addressable Remove addressableId: {addressableId} _addressable:{Addressable.Count}");
            }
        }

        /// <summary>
        /// 锁定地址映射。
        /// </summary>
        /// <param name="addressableId">地址映射的唯一标识。</param>
        public async FTask Lock(long addressableId)
        {
            var waitCoroutineLock = await AddressableLock.Lock(addressableId);
            Locks.Add(addressableId, waitCoroutineLock);
        }

        /// <summary>
        /// 解锁地址映射。
        /// </summary>
        /// <param name="addressableId">地址映射的唯一标识。</param>
        /// <param name="routeId">新的路由 ID。</param>
        /// <param name="source">解锁来源。</param>
        public void UnLock(long addressableId, long routeId, string source)
        {
            if (!Locks.Remove(addressableId, out var coroutineLock))
            {
                Log.Error($"Addressable unlock not found addressableId: {addressableId} Source:{source}");
                return;
            }

            Addressable.TryGetValue(addressableId, out var oldAddressableId);

            if (routeId != 0)
            {
                Addressable[addressableId] = routeId;
            }

            coroutineLock.Dispose();
            Log.Debug($"Addressable UnLock key: {addressableId} oldAddressableId : {oldAddressableId} routeId: {routeId}  Source:{source}");
        }
    }
}
#endif