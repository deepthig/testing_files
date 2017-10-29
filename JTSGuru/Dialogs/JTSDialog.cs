using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Linq;
using System.Collections.Generic;

namespace JTSGuru.Dialogs
{

    [Serializable]
    public class Option
    {
        public int ID { get; set; }
        public string Text { get; set; }
        public Option()
        {
            ID = 0;
            Text = "";
        }

        public override string ToString()
        {
            return this.ID.ToString();
        }
        public static List<Option> CreateListOption()
        {
            List<Option> Options = new List<Option>();
            Option A = new Option();
            A.ID = 1;
            A.Text = "CP";
            Option B = new Option();
            B.ID = 2;
            B.Text = "PB";
            Options.Add(A);
            Options.Add(B);
            return Options;
        }
    }

    [Serializable]
    public class JTSDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.UserData.SetValue<int>("ProductCode", 0);
            context.UserData.SetValue<int>("JTSNumber", 0);
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private int GetJTSNumber(String result)
        {
            var JTSNumberString = new String(result.Where(Char.IsDigit).ToArray());
            if (!String.IsNullOrEmpty(JTSNumberString))
            {
                int JTSNumber = int.Parse(JTSNumberString);
                if (JTSNumber > 0)
                {
                    return JTSNumber;
                }
            } 
            return 0;
        }
        private bool CheckifJTS(Activity result)
        {
            var activity = result;
            if (activity.Text.IndexOf("JTS", 0, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            return false;
        }

        private async Task StoreJTSNumber(IDialogContext context, IAwaitable<object> result)
        {
            int JTSNumber = GetJTSNumber(result.ToString());
            if (JTSNumber > 0)
            {
                context.UserData.SetValue("JTSNumber", JTSNumber);
                await ConstructJTSLink(context);
            }
            else
                PromptDialog.Text(context, StoreJTSNumber, "I am sorry! That was not a JTS number");
            
        }

        private async Task StoreProductCode(IDialogContext context, IAwaitable<object> result)
        {
            int ProductCode = int.Parse(result.ToString());
            
            //int ProductCode = GetJTSNumber(result.ToString());
            if (ProductCode > 0)
            {
                context.UserData.SetValue("ProductCode", ProductCode);
                await ConstructJTSLink(context);
            }
            else
                PromptDialog.Text(context, StoreProductCode, "I am sorry! That was not a Product Code");
            
        }

        private async Task ConstructJTSLink(IDialogContext context)
        {
            int JTSNumber = context.UserData.GetValue<int>("JTSNumber");
            int ProductCode = context.UserData.GetValue<int>("ProductCode");
            if (ProductCode > 0 && JTSNumber > 0)
            {
                String JTSLink = String.Format($"http://jts.ingrnet.com/report.asp?ProductGroup={ProductCode}&JTSNo={JTSNumber}&view=executive&voEdit=ON&voChildren=ON");

                await context.PostAsync($"Your JTS Link is ready \n " + JTSLink);
                context.Wait(MessageReceivedAsync);
            }
            else
            {
                if (JTSNumber <= 0)
                {
                    PromptDialog.Text(context, StoreJTSNumber, "Please provide me a JTS Number");
                }
                if (ProductCode <= 0)
                {
                    List<Option> myOptions = Option.CreateListOption();
                    //PromptDialog.Choice(context, this.StoreProductCode, myOptions, "Are you searching for CP or PB", "Not a valid option", 3);
                    PromptDialog.Text(context, StoreProductCode, "Please provide me a product code");
                }
                
            }
        }
        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            if (String.Compare(activity.Text, "Thank you") == 0)
            {
                await context.PostAsync($"Thank you for using JTS Guru. ");
                //context.UserData.Clear();
                context.Done(this);
            }
            else if (!CheckifJTS(activity))
            {
                await context.PostAsync($"I am JTS Guru. Here to help you. Please ask me anything about JTS");
                context.Wait(MessageReceivedAsync);
            }
            else
            {
                int JTSNumber = GetJTSNumber(activity.Text);
                if (JTSNumber > 0)
                {
                    context.UserData.SetValue("JTSNumber", JTSNumber);
                    await ConstructJTSLink(context);
                    //Display the JTS Number
                }
                else
                {
                    context.UserData.SetValue("JTSNumber", 0);
                    PromptDialog.Text(context, StoreJTSNumber, "Without number how will I give you a link. Please provide me a JTS Number");
                    //context.Wait(MessageReceivedAsync);
                }
            }
            
            
        }
    }
}