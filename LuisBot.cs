// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// For each interaction from the user, an instance of this class is created and
    /// the OnTurnAsync method is called.
    /// </summary>
    public class LuisBot : IBot
    {
        /// <summary>
        /// Key in the bot config (.bot file) for the LUIS instance.
        /// In the .bot file, multiple instances of LUIS can be configured.
        /// </summary>
        public static readonly string LuisKey = "LuisBot";

        private const string WelcomeText = "This bot will introduce you to natural language processing with LUIS. Type an utterance to get started";

        /// <summary>
        /// Services configured from the ".bot" file.
        /// </summary>
        private readonly BotServices _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisBot"/> class.
        /// </summary>
        /// <param name="services">Services configured from the ".bot" file.</param>
        public LuisBot(BotServices services)
        {
            _services = services ?? throw new System.ArgumentNullException(nameof(services));
            if (!_services.LuisServices.ContainsKey(LuisKey))
            {
                throw new System.ArgumentException($"Invalid configuration. Please check your '.bot' file for a LUIS service named '{LuisKey}'.");
            }
        }

        /// <summary>
        /// Every conversation turn for our LUIS Bot will call this method.
 
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Check LUIS model
                var recognizerResult = await _services.LuisServices[LuisKey].RecognizeAsync(turnContext, cancellationToken);
                var topIntent = recognizerResult?.GetTopScoringIntent();
                var entities = recognizerResult?.Entities;
                if (entities["number"] != null)
                {
                    var num = (string)entities["number"].First;
                }

                if (topIntent != null && topIntent.HasValue && topIntent.Value.intent != "None")
                {
                    var msg = @"22 Hours";
                    await turnContext.SendActivityAsync(msg);
                }
                else
                {
                    var didNotUnderstandCard = CreateDidNotUnderstandAdaptiveCardAttachment();
                    var response = turnContext.Activity.CreateReply();
                    response.Attachments = new List<Attachment>() { didNotUnderstandCard };
                    await turnContext.SendActivityAsync(response);
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                // Send a welcome message to the user and tell them what actions they may perform to use this bot
                await SendWelcomeMessageAsync(turnContext, cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected", cancellationToken: cancellationToken);
            }
        }

        private static Attachment CreateWelcomeAdaptiveCardAttachment()
        {
            var adaptiveCard = File.ReadAllText(@".\welcomeCard.json");
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };
        }

        private static Attachment CreateDidNotUnderstandAdaptiveCardAttachment()
        {
            var adaptiveCard = File.ReadAllText(@".\didNotUnderstandCard.json");
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };
        }


        /// <summary>
        /// On a conversation update activity sent to the bot, the bot will
        /// send a message to the any new user(s) that were added.
        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var welcomeCard = CreateWelcomeAdaptiveCardAttachment();
                    var response = turnContext.Activity.CreateReply();
                    response.Attachments = new List<Attachment>() { welcomeCard };
                    await turnContext.SendActivityAsync(response);
                }
            }
        }
    }
}
