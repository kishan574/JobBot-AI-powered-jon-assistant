using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using LuisBot.Model;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

namespace Microsoft.Bot.Sample.LuisBot
{
    // For more information about this template visit http://aka.ms/azurebots-csharp-luis
    [Serializable]
    public class BasicLuisDialog : LuisDialog<object>
    {
        static readonly JobState jobState = new JobState();
        static readonly EntityState entityState = new EntityState();
        static readonly UserState userState = new UserState();
        private string luisEnt;
        EntityRecommendation industry;
        EntityRecommendation UserName;


        public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(
            ConfigurationManager.AppSettings["LuisAppId"], 
            ConfigurationManager.AppSettings["LuisAPIKey"], 
            domain: ConfigurationManager.AppSettings["LuisAPIHostName"])))
        {
        }

        [LuisIntent("Greeting")]
        public async Task GreetingIntent(IDialogContext context, LuisResult result)
        {
            //context.ConversationData.SetValue("myConversationdata", "" );
            //context.UserData.SetValue("User", UserName);

            if (!result.TryFindEntity("userName", out UserName))
            {
                PromptDialog.Text(context, AfterUsernamePrompt, $"Hello, What's your name?", attempts: 2);
            }
            else
            {
                await context.PostAsync($"Hey {UserName.Entity}, what can i do for you?");
                context.UserData.SetValue(UserName.Type, UserName.Entity);
            }

        }

        public async Task AfterUsernamePrompt(IDialogContext context, IAwaitable<string> result)
        {
            var name = await result;
            userState.Username = name;
            if (name != null)
            {
                context.UserData.SetValue("userName", userState.Username);
                await context.PostAsync($"How can i help you {userState.Username}?");
            }
        }

        [LuisIntent("Cancel")]  //to cancel the conversation state. 
        public async Task CancelIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Action has been canceled.");
        }

        [LuisIntent("Help")]
        public async Task HelpIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("I can find the right jobs for you.\njust start typing the job you are looking for.");
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("I am sorry, I did not understand.");
        }

        [LuisIntent("JobFind")]
        public async Task JobFindIntent(IDialogContext context, LuisResult result)
        {
            luisEnt = entityState.BotEntityRecognition(result);

            if (!result.TryFindEntity("Industry", out industry))
            {
                PromptDialog.Text(context, AfterJobFind, "What kind of job are you looking for?\nPlease specify Industry.", attempts: 10);
            }

            else
            {
                PromptDialog.Text(context, AfterJobIndustry, "list you key skills\n provide a comma after each skill.", attempts: 10);
            }

        }

        private async Task AfterJobFind(IDialogContext context, IAwaitable<string> result)
        {
            var indst = await result;
            jobState.Industry = indst;
            if (indst != null)
            {
                PromptDialog.Text(context, AfterJobIndustry, "list you key skills\n provide a comma after each skill.", attempts: 10);

            }
        }

        private async Task AfterJobIndustry(IDialogContext context, IAwaitable<string> result)
        {
            var skill = await result;
            jobState.SkillStr = skill;
            string query = "";
            List<string> skillList = jobState.SkillStr.Split(',').ToList();

            context.ConversationData.SetValue(industry.Type, industry.Entity);

            context.ConversationData.SetValue("Entities", luisEnt);

            for (int i = 0; i < skillList.Count; i++)
            {
                string sList = skillList[i];
                context.ConversationData.SetValue($"skill{i}", sList);
            }

            for(int i=0; i < skillList.Count; i++)
            {
                query = jobState.SkillStr.Replace(',','&');
            }


            await context.PostAsync($"Ok! you are looking for Job in **{industry.Entity}** with these skills.\n **{jobState.SkillStr}**");
            await context.PostAsync("Please visit the following link");
            var url = "https://localhost/result?industry=" + industry.Entity + "&" + "skills=" + query;

            if (url.Contains(' '))
            {
                url = url.Replace(' ','+');
            }
            await context.PostAsync(url);
        }

    }
}