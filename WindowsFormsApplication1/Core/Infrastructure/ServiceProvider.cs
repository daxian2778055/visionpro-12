using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1.Core.Infrastructure
{
    /// <summary>
    /// 极简服务容器（组合根实现）。仅提供注册/解析单例，足够本项目服务化改造使用，
    /// 不引入第三方 DI 依赖。后续如需更复杂生命周期（瞬时/作用域），可整体替换为成熟容器，
    /// 接口保持 <see cref="Resolve{T}"/> / <see cref="RegisterSingleton{T}(T)"/> 不变即可。
    /// </summary>
    public sealed class ServiceProvider
    {
        private readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
        private readonly object _sync = new object();

        /// <summary>注册一个已创建的单例实例。</summary>
        public void RegisterSingleton<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException("instance");
            lock (_sync) _singletons[typeof(T)] = instance;
        }

        /// <summary>注册一个工厂；首次 <see cref="Resolve{T}"/> 时创建并缓存为单例。</summary>
        public void RegisterSingleton<T>(Func<T> factory) where T : class
        {
            if (factory == null) throw new ArgumentNullException("factory");
            lock (_sync) _factories[typeof(T)] = () => factory();
        }

        /// <summary>解析服务；未注册时抛 <see cref="InvalidOperationException"/>。</summary>
        public T Resolve<T>() where T : class
        {
            object instance;
            if (!TryResolveInternal(typeof(T), out instance))
                throw new InvalidOperationException("未注册的服务类型: " + typeof(T).FullName);
            return (T)instance;
        }

        /// <summary>尝试解析；失败返回 false 且不抛异常。</summary>
        public bool TryResolve<T>(out T instance) where T : class
        {
            object obj;
            if (TryResolveInternal(typeof(T), out obj))
            {
                instance = (T)obj;
                return true;
            }
            instance = null;
            return false;
        }

        private bool TryResolveInternal(Type type, out object instance)
        {
            // ★ N2：工厂执行移到锁外，避免持锁调用用户工厂导致跨线程嵌套 Resolve 死锁。
            //   持锁只做字典查找 + 写缓存；工厂在锁外执行，写缓存时幂等单例化
            //   （并发重复解析时后写覆盖，值一致，语义不变）。
            lock (_sync)
            {
                if (_singletons.TryGetValue(type, out instance)) return true;
            }

            Func<object> factory;
            lock (_sync)
            {
                if (_singletons.TryGetValue(type, out instance)) return true; // 双检：其它线程可能已创建
                if (!_factories.TryGetValue(type, out factory))
                {
                    instance = null;
                    return false;
                }
            }

            instance = factory();          // 锁外执行用户工厂
            lock (_sync)                   // 写缓存
            {
                _singletons[type] = instance;
            }
            return true;
        }

        /// <summary>
        /// ★ N4/N3：释放所有已实例化的单例服务（仅处理实现 <see cref="IDisposable"/> 的），
        /// 并清空容器，供组合根在退出时统一收口。逐个容错，可重复调用。
        /// </summary>
        public void DisposeAll()
        {
            List<object> instances;
            lock (_sync)
            {
                instances = new List<object>(_singletons.Values);
                _singletons.Clear();
                _factories.Clear();
            }

            foreach (object o in instances)
            {
                var disposable = o as IDisposable;
                if (disposable != null)
                {
                    try { disposable.Dispose(); } catch { }
                }
            }
        }
    }
}
