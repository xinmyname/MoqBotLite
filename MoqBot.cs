using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;

namespace MoqBotLite
{
    public class MoqBot : IDisposable {

        private interface IResolveTypes {
            object Resolve();
        }

        private class ConstantResolver : IResolveTypes {

            private readonly object _value;

            public ConstantResolver(object value) {
                _value = value;
            }

            public object Resolve() {
                return _value;
            }
        }

        private class DynamicResolver<T> : IResolveTypes {

            private readonly Func<T> _builder;

            public DynamicResolver(Func<T> builder) {
                _builder = builder;
            }

            public object Resolve() {
                return _builder();
            }
        }

        private readonly MockRepository _factory;
        private readonly IDictionary<Type,IResolveTypes> _bindings;

        private bool _disposedValue = false;

        public MoqBot()
            : this(MockBehavior.Default) {
        }

        public MoqBot(MockBehavior mockBehavior)
            : this(new MockRepository(mockBehavior)) {
        }

        public MoqBot(MockRepository factory) {
            _factory = factory;
            _bindings = new Dictionary<Type,IResolveTypes>();
        }

        public Mock<I> Mock<I>(MockBehavior behavior) where I : class {

            Mock<I> mock = _factory.Create<I>(behavior);

            _bindings[typeof(I)] = new ConstantResolver(mock.Object);

            return mock;
        }

        public Mock<I> Mock<I>() where I : class {
            return Mock<I>(MockBehavior.Default);
        }

        public Mock<I> Stub<I>() where I : class {
            return Mock<I>(MockBehavior.Loose);
        }

        public void Verify() {
            _factory.Verify();
        }

        public void Register<T>(Func<T> builder) {
            _bindings[typeof(T)] = new DynamicResolver<T>(builder);
        }

        public T Get<T>() {

            ConstructorInfo constructorInfo = FindConstructorFor<T>();
            ParameterInfo[] constructorParams = constructorInfo.GetParameters();
            var mockParams = new object[constructorParams.Length];
            
            for (int i = 0; i < constructorParams.Length; i++) {

                Type paramType = constructorParams[i].ParameterType;

                if (_bindings.ContainsKey(paramType)) {
                    IResolveTypes resolver = _bindings[paramType];
                    mockParams[i] = resolver.Resolve();
                } else if (paramType.IsValueType) {
                    mockParams[i] = Activator.CreateInstance(paramType);
                }
            }

            return (T)constructorInfo.Invoke(mockParams.ToArray());
        }

        protected virtual void Dispose(bool disposing) {

            if (!_disposedValue) {

                if (disposing)
                    Verify();

                _disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        private ConstructorInfo FindConstructorFor<T>() {

            Type type = typeof(T);
            ConstructorInfo result = null;
            int resultScore = -1;
            
            foreach (ConstructorInfo current in type.GetConstructors(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)) {

                int currentScore = ScoreConstructor(current);

                if (currentScore > resultScore) {
                    result = current;
                    resultScore = currentScore;
                }
            }

            return result;
        }

        private int ScoreConstructor(ConstructorInfo constructorInfo) {

            int score = 0;

            foreach (ParameterInfo paramInfo in constructorInfo.GetParameters()) {

                score++;

                if (_bindings.ContainsKey(paramInfo.ParameterType))
                    score++;
            }

            return score;
        }
    }
}