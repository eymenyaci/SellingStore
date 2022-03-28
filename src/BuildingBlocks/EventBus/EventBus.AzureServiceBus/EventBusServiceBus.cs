using EventBus.Base;
using EventBus.Base.Event;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.AzureServiceBus
{
    public class EventBusServiceBus : BaseEventBus
    {
        private ITopicClient topicClient;
        private ManagementClient managementClient;
        private ILogger logger;

        #region ctor
        public EventBusServiceBus(EventBusConfig config, IServiceProvider serviceProvider) : base(config, serviceProvider)
        {
            logger = serviceProvider.GetService(typeof(ILogger<EventBusServiceBus>)) as ILogger<EventBusServiceBus>;
            managementClient = new ManagementClient(config.EventBusConnectionString);
            topicClient = CreateTopicClient();
        }
        #endregion

        #region create and checking topic
        private ITopicClient CreateTopicClient()
        {
            if (topicClient == null || topicClient.IsClosedOrClosing)
            {
                topicClient = new TopicClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, RetryPolicy.Default);
            }
            //Ensure that topic already exists
            if (!managementClient.TopicExistsAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult())
            {
                managementClient.CreateTopicAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult();
            }
            return topicClient;

        }
        #endregion

        #region Publish Method
        public override void Publish(IntegrationEvent @event)
        {
            var eventName = @event.GetType().Name; // example : OrderCreatedIntegrationEvent
            eventName = ProcessEventName(eventName); // example : OrderCreated
            var eventStr = JsonConvert.SerializeObject(@event);
            var bodyArr = Encoding.UTF8.GetBytes(eventStr);


            var message = new Message()
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = null,
                Label = eventName

            };
            topicClient.SendAsync(message).GetAwaiter().GetResult();

        }
        #endregion

        #region Subs Method
        public override void Subscribe<T, TH>()
        {
            var eventName = typeof(T).Name;
            eventName = ProcessEventName(eventName);
            if (!SubsManager.HasSubscriptionForEvent(eventName))
            {
                var subscriptionClient = CreateSubscriptionClientIfNotExists(eventName);
                RegisterSubscriptionClientMessageHandler(subscriptionClient);
            }

            SubsManager.AddSubscription<T, TH>();
            logger.LogInformation("Subscribing to event {eventName} with {EventHandler}", eventName, typeof(TH).Name);


        }
        #endregion

        #region unSubs Method
        public override void UnSubscribe<T, TH>()
        {
            var eventName = typeof(T).Name;

            try
            {
                //Subscription will be there but we don't subscribe
                var subscriptionClient = CreateSubscriptionClient(eventName);
                subscriptionClient
                    .RemoveRuleAsync(eventName)
                    .GetAwaiter()
                    .GetResult();

            }
            catch (MessagingEntityNotFoundException)
            {
                logger.LogWarning("The messaging entitiy {eventName} could not be found.", eventName);
            }

            logger.LogInformation("Unsubscribing from event {eventName}", eventName);
            SubsManager.RemoveSubscription<T, TH>();    

        }
        #endregion

        #region Create subs and rule methods
        private void RegisterSubscriptionClientMessageHandler(ISubscriptionClient subscriptionClient)
        {
            subscriptionClient.RegisterMessageHandler(
                async (message, token) =>
                {
                    var eventName = $"{message.Label}";
                    var messageData = Encoding.UTF8.GetString(message.Body);

                    // Complete the message so that it is not received again.
                    if (await ProcessEvent(ProcessEventName(eventName), messageData))
                    {
                        await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                    }
                },
                new MessageHandlerOptions(ExceptionReceivedHandler) { MaxConcurrentCalls = 10, AutoComplete = false });
        }
        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var ex = exceptionReceivedEventArgs.Exception;
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            logger.LogError(ex, "ERROR handling message: {ExceptionMessage} - Context: {@ExceptionContext}", ex.Message, context);

            return Task.CompletedTask;
        }
        private SubscriptionClient CreateSubscriptionClient(string eventName)
        {
            return new SubscriptionClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, GetSubName(eventName));
        }

        private ISubscriptionClient CreateSubscriptionClientIfNotExists(string eventName)
        {
            var subClient = CreateSubscriptionClient(eventName);
            var exists = managementClient.SubscriptionExistsAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();
            if (!exists)
            {
                managementClient.CreateSubscriptionAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();
                RemoveDefaultRule(subClient);
            }
            CreateRuleIfNotExists(ProcessEventName(eventName), subClient);
            return subClient;
        }

        private void CreateRuleIfNotExists(string eventName, ISubscriptionClient subscriptionClient)
        {
            bool ruleExists;
            try
            {
                var rule = managementClient.GetRuleAsync(EventBusConfig.DefaultTopicName, eventName, eventName).GetAwaiter().GetResult();
                ruleExists = rule != null; // rule null değilse ruleExists True dön

            }
            catch (MessagingEntityNotFoundException)
            {
                //Azure Management Client doesn't have RuleExists method
                ruleExists = false;
            }
            if (!ruleExists)
            {
                subscriptionClient.AddRuleAsync(new RuleDescription
                {
                    Filter = new CorrelationFilter { Label = eventName },
                    Name = eventName
                }).GetAwaiter().GetResult();
            }
        }

        private void RemoveDefaultRule(SubscriptionClient subscriptionClient)
        {
            try
            {
                subscriptionClient
                    .RemoveRuleAsync(RuleDescription.DefaultRuleName)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {

                logger.LogWarning("The messaging entitiy {DefaultRoleName} could not be found.", RuleDescription.DefaultRuleName);
            }
        }
        #endregion

        #region override method
        public override void Dispose()
        {
            base.Dispose();
            topicClient = null;
            managementClient = null;
            topicClient.CloseAsync().GetAwaiter().GetResult();
            topicClient = null;
            managementClient = null;
        }
        #endregion

    }
}
