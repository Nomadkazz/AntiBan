using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Xunit.Sdk;

namespace Antiban
{
    public class Antiban
    {
        private Dictionary<DateTime, string> phoneNumberForTimeSlot = new Dictionary<DateTime, string>();
        private List<EventMessage> messages = new List<EventMessage>();

        /// <summary>
        /// Добавление сообщений в систему, для обработки порядка сообщений
        /// </summary>
        /// <param name="eventMessage"></param>
        public void PushEventMessage(EventMessage eventMessage)
        {
            messages.Add(eventMessage);
        }

        /// <summary>
        /// Вовзращает порядок отправок сообщений
        /// </summary>
        /// <returns></returns>
        public List<AntibanResult> GetResult()
        {
            phoneNumberForTimeSlot = new Dictionary<DateTime, string>();
            Dictionary<string, List<DateTime>> timesForPhoneNumber = new Dictionary<string, List<DateTime>>();
            Dictionary<string, DateTime> lastSentTimeToAnyNumber = new Dictionary<string, DateTime>();
            Dictionary<string, DateTime> lastSentTimeToNumberPriority1 = new Dictionary<string, DateTime>();
            var result = new List<AntibanResult>();
            
            messages.Sort((x, y) => DateTime.Compare(x.DateTime, y.DateTime));
            foreach (var eventMessage in messages)
            {
                string phoneNumber = eventMessage.Phone;
                int priority = eventMessage.Priority;
                DateTime thisEventTime = eventMessage.DateTime;

                if (priority == 1)
                {
                    if (lastSentTimeToNumberPriority1.ContainsKey(phoneNumber))
                    {
                        var lastSentTime = lastSentTimeToNumberPriority1.GetValueOrDefault(phoneNumber);
                            
                        if ((thisEventTime - lastSentTime).TotalHours < 24)
                        {
                            thisEventTime = lastSentTime.AddHours(24);
                        }
                    }
                    else if(!lastSentTimeToNumberPriority1.ContainsKey(phoneNumber) && lastSentTimeToAnyNumber.ContainsKey(phoneNumber))
                    {
                        var lastSentTime = lastSentTimeToAnyNumber.GetValueOrDefault(phoneNumber);

                        if ((thisEventTime - lastSentTime).TotalSeconds < 60)
                        {
                            thisEventTime = lastSentTime.AddSeconds(60);
                        }
                    }
                }
                else if (priority == 0)
                {
                    //phoneNumber exists
                    if (lastSentTimeToNumberPriority1.ContainsKey(phoneNumber) || lastSentTimeToAnyNumber.ContainsKey(phoneNumber))
                    {
                        DateTime lastSentTime = new DateTime();
                        if (lastSentTimeToNumberPriority1.ContainsKey(phoneNumber) && lastSentTimeToAnyNumber.ContainsKey(phoneNumber))
                        {
                            var times = timesForPhoneNumber[phoneNumber];
                            var nearestTime = times[0];
                            var nearestTimeInterval = Math.Abs((thisEventTime - times[0]).TotalSeconds);
                            foreach (var time in times)
                            {
                                var newTimeInterval = Math.Abs((thisEventTime - time).TotalSeconds);
                                if (newTimeInterval < nearestTimeInterval)
                                {
                                    nearestTimeInterval = newTimeInterval;
                                    nearestTime = time;
                                }
                            }
                            lastSentTime = nearestTime;
                        }
                        else if (!lastSentTimeToAnyNumber.ContainsKey(phoneNumber) && lastSentTimeToNumberPriority1.ContainsKey(phoneNumber))
                        {
                            lastSentTime = lastSentTimeToNumberPriority1.GetValueOrDefault(phoneNumber);
                        }
                        else if (lastSentTimeToAnyNumber.ContainsKey(phoneNumber) && !lastSentTimeToNumberPriority1.ContainsKey(phoneNumber))
                        {
                            lastSentTime = lastSentTimeToAnyNumber.GetValueOrDefault(phoneNumber);
                        }

                        if ((thisEventTime - lastSentTime).TotalSeconds < 60)
                        {
                            thisEventTime = lastSentTime.AddSeconds(60);
                        }
                    }
                }
                
                thisEventTime = getTimeSlot(thisEventTime);

                if (timesForPhoneNumber.ContainsKey(phoneNumber))
                {
                    timesForPhoneNumber[phoneNumber].Add(thisEventTime);
                }
                else
                {
                    timesForPhoneNumber[phoneNumber] = new List<DateTime>() { thisEventTime };
                }
            
                if (priority == 1)
                {
                    lastSentTimeToNumberPriority1[phoneNumber] = thisEventTime;
                }
                else
                {
                    lastSentTimeToAnyNumber[phoneNumber] = thisEventTime;
                }

                phoneNumberForTimeSlot[thisEventTime] = phoneNumber;

                result.Add(new AntibanResult
                {
                    SentDateTime = thisEventTime,
                    EventMessageId = eventMessage.Id,
                });

            }
            
            result.Sort((x, y) => DateTime.Compare(x.SentDateTime, y.SentDateTime));
            
            return result;

        }

        /// <summary>
        /// Checks if desired time slot, and time slots within the range of 10 seconds are not occupied.
        /// Otherwise updates the time slot so that it satisfies the condition.
        /// </summary>
        /// <param name="thisDateTime">Desired time slot.</param>
        /// <returns>
        /// DateTime, which satisfies the condition.
        /// </returns>
        private DateTime getTimeSlot(DateTime thisDateTime)
        {            

            if (!phoneNumberForTimeSlot.ContainsKey(thisDateTime))
            {
                var startRange = thisDateTime.AddSeconds(-9);
                while (!phoneNumberForTimeSlot.ContainsKey(startRange) && startRange < thisDateTime)
                {
                    startRange = startRange.AddMilliseconds(1);
                }
                if (startRange != thisDateTime)
                {
                    thisDateTime = startRange.AddSeconds(10);
                }
            }

            var temp = thisDateTime;
            var endRange = thisDateTime.AddSeconds(10);
            while (temp < endRange)
            {
                if (!phoneNumberForTimeSlot.ContainsKey(temp))
                {
                    temp = temp.AddMilliseconds(1);
                }
                else if (phoneNumberForTimeSlot.ContainsKey(temp) && temp < endRange)
                {

                    thisDateTime = temp.AddSeconds(10);
                    temp = thisDateTime;
                    endRange = thisDateTime.AddSeconds(10);
                }
            }
            
            return thisDateTime;
        }
    
    }
}
