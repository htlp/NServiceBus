﻿namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Features;
    using EndpointTemplates;
    using NUnit.Framework;
    using NServiceBus.Transport;

    class When_subscribed_to_OnProcessingCompleted : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_notifications_for_successfull()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SubscribingEndpoint>(b => b.When(session => session.SendLocal(new SomeMessage())))
                .Done(c => c.Event != null)
                .Run();

            Assert.NotNull(context.Event, "Event was not raised");
            Assert.AreEqual(context.MessageId, context.Event.MessageId, "MessageId should match");
            Assert.True(context.Event.WasAcknowledged, "Should be flagged as acknowledged");
            Assert.AreNotEqual(DateTime.MinValue, context.Event.StartedAt, "StartedAt was not set");
            Assert.AreNotEqual(DateTime.MinValue, context.Event.CompletedAt, "CompletedAt was not set");
        }

        [Test]
        public async Task Should_receive_notifications_for_rollback()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SubscribingEndpoint>(b =>
                {
                    //do one immediate retry to check that message is rolled back
                    b.CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(1)));

                    //message will then go to error so we need to allow that
                    b.DoNotFailOnErrorMessages();

                    b.When(session => session.SendLocal(new SomeMessage { Throw = true }));
                })
                .Done(c => c.Event != null)
                .Run();

            Assert.NotNull(context.Event, "Event was not raised");
            Assert.AreEqual(context.MessageId, context.Event.MessageId, "MessageId should match");
            Assert.False(context.Event.WasAcknowledged, "Should be rolled back");
            Assert.AreNotEqual(DateTime.MinValue, context.Event.StartedAt, "StartedAt was not set");
            Assert.AreNotEqual(DateTime.MinValue, context.Event.CompletedAt, "CompletedAt was not set");
        }

        class Context : ScenarioContext
        {
            public ProcessingCompleted Event { get; set; }
            public string MessageId { get; set; }
        }

        class SubscribingEndpoint : EndpointConfigurationBuilder
        {
            public SubscribingEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class EventEnablingFeature : Feature
            {
                public EventEnablingFeature()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.OnMessageProcessingCompleted((e, _) =>
                    {
                        var testContext = (Context)context.Settings.Get<ScenarioContext>();

                        if (testContext.Event == null)
                        {
                            testContext.Event = e;
                        }

                        return Task.CompletedTask;
                    });
                }
            }

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                public SomeMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    if (testContext.Event != null)
                    {
                        return Task.CompletedTask;
                    }

                    testContext.MessageId = context.Extensions.Get<IncomingMessage>().NativeMessageId;

                    if (message.Throw)
                    {
                        throw new Exception("Simulated failure");
                    }

                    return Task.CompletedTask;
                }

                Context testContext;
            }
        }

        public class SomeMessage : IMessage
        {
            public bool Throw { get; set; }
        }
    }
}
