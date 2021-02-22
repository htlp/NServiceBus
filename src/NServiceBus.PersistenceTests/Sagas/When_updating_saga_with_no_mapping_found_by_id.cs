﻿namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    public class When_updating_saga_with_no_mapping_found_by_id : SagaPersisterTests
    {
        [Test]
        public async Task It_should_successfully_update_the_saga()
        {
            configuration.RequiresFindersSupport();

            var sagaData = new SagaWithoutCorrelationPropertyData
            {
                SomeSagaProperty = Guid.NewGuid().ToString(),
            };

            await SaveSaga(sagaData);

            var updateValue = Guid.NewGuid().ToString();
            var context = configuration.GetContextBagForSagaStorage();
            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context, default))
            {
                sagaData = await configuration.SagaStorage.Get<SagaWithoutCorrelationPropertyData>(sagaData.Id, completeSession, context, default);
                sagaData.SomeSagaProperty = updateValue;

                await configuration.SagaStorage.Update(sagaData, completeSession, context, default);
                await completeSession.CompleteAsync(default);
            }

            var result = await GetById<SagaWithoutCorrelationPropertyData>(sagaData.Id);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.SomeSagaProperty, Is.EqualTo(updateValue));
        }

        public class SagaWithoutCorrelationProperty : Saga<SagaWithoutCorrelationPropertyData>,
            IAmStartedByMessages<SagaWithoutCorrelationPropertyStartingMessage>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithoutCorrelationPropertyData> mapper)
            {
                // no mapping needed
            }

            public Task Handle(SagaWithoutCorrelationPropertyStartingMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }
        }

        public class CustomFinder : IFindSagas<SagaWithoutCorrelationPropertyData>.Using<SagaWithoutCorrelationPropertyStartingMessage>
        {
            public Task<SagaWithoutCorrelationPropertyData> FindBy(SagaWithoutCorrelationPropertyStartingMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        public class SagaWithoutCorrelationPropertyData : ContainSagaData
        {
            public string SomeSagaProperty { get; set; }
        }

        public class SagaWithoutCorrelationPropertyStartingMessage : IMessage
        {
        }

        public When_updating_saga_with_no_mapping_found_by_id(TestVariant param) : base(param)
        {
        }
    }
}