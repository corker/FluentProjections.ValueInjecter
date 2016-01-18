using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentProjections.Persistence;
using NUnit.Framework;

namespace FluentProjections.ValueInjecter.Tests
{
    public class MessageHandlerTests
    {
        private class TestEvent
        {
            public short ValueInt16 { get; set; }
            public int ValueInt32 { get; set; }
            public long ValueInt64 { get; set; }
        }

        private class TestProjection
        {
            public short ValueInt16 { get; set; }
            public int ValueInt32 { get; set; }
            public long ValueInt64 { get; set; }
        }

        private class TestPersistenceFactory : ICreateProjectionPersistence
        {
            private readonly IPersistProjections _persistence;

            public TestPersistenceFactory(IPersistProjections persistence)
            {
                _persistence = persistence;
            }

            public async Task<IPersistProjections> Create()
            {
                return await Task.FromResult(_persistence);
            }
        }

        private class TestPersistence : IPersistProjections
        {
            public TestPersistence(TestProjection readProjection)
            {
                ReadProjection = readProjection;
            }

            public IEnumerable<FilterValue> ReadFilterValues { get; private set; }
            public TestProjection ReadProjection { get; }
            public TestProjection UpdateProjection { get; private set; }
            public List<TestProjection> InsertProjections { get; private set; }
            public IEnumerable<FilterValue> RemoveFilterValues { get; private set; }

            public async Task<IEnumerable<TProjection>> Read<TProjection>(IEnumerable<FilterValue> values)
                where TProjection : class
            {
                ReadFilterValues = values;
                return await Task.FromResult(new[] {ReadProjection}.OfType<TProjection>());
            }

            public async Task Update<TProjection>(TProjection projection) where TProjection : class
            {
                UpdateProjection = projection as TestProjection;
                await Task.Yield();
            }

            public async Task Insert<TProjection>(TProjection projection) where TProjection : class
            {
                InsertProjections = InsertProjections ?? new List<TestProjection>();
                InsertProjections.Add(projection as TestProjection);
                await Task.Yield();
            }

            public async Task Remove<TProjection>(IEnumerable<FilterValue> values) where TProjection : class
            {
                RemoveFilterValues = values;
                await Task.Yield();
            }
        }

        [TestFixture]
        public class When_event_add_new_projection_and_inject_properties
        {
            private class TestHandler : MessageHandler<TestProjection>
            {
                public TestHandler(ICreateProjectionPersistence persistenceFactory) : base(persistenceFactory)
                {
                }

                public async Task Handle(TestEvent @event)
                {
                    await Handle(@event, x => x.AddNew().Inject());
                }
            }

            private TestPersistence _targetPersistence;

            [OneTimeSetUp]
            public void Init()
            {
                var @event = new TestEvent
                {
                    ValueInt32 = 777
                };

                _targetPersistence = new TestPersistence(null);
                var persistenceFactory = new TestPersistenceFactory(_targetPersistence);
                new TestHandler(persistenceFactory).Handle(@event).Wait();
            }

            [Test]
            public void Should_add_new_projection()
            {
                Assert.AreEqual(1, _targetPersistence.InsertProjections.Count);
            }

            [Test]
            public void Should_map_values()
            {
                Assert.AreEqual(777, _targetPersistence.InsertProjections.Single().ValueInt32);
            }
        }
    }
}