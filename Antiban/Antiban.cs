using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using System;
using System.Collections.Generic;
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
            Dictionary<string, List<DateTime>> sentTimesForPhoneNumber = new Dictionary<string, List<DateTime>>();
            Dictionary<string, DateTime> lastSentTimeToNumberPriorityZero = new Dictionary<string, DateTime>();
            Dictionary<string, DateTime> lastSentTimeToNumberPriorityOne = new Dictionary<string, DateTime>();
            var result = new List<AntibanResult>();
            
            foreach (var eventMessage in messages)
            {
                string phoneNumber = eventMessage.Phone;
                int priority = eventMessage.Priority;
                DateTime thisEventTime = eventMessage.DateTime;

                if (priority == 1)
                {
                    if (lastSentTimeToNumberPriorityOne.ContainsKey(phoneNumber))
                    {
                        var lastSentTime = lastSentTimeToNumberPriorityOne.GetValueOrDefault(phoneNumber);
                            
                        if ((thisEventTime - lastSentTime).TotalHours < 24)
                        {
                            thisEventTime = lastSentTime.AddHours(24);
                        }
                    }
                    else if(!lastSentTimeToNumberPriorityOne.ContainsKey(phoneNumber) && lastSentTimeToNumberPriorityZero.ContainsKey(phoneNumber))
                    {
                        var lastSentTime = lastSentTimeToNumberPriorityZero.GetValueOrDefault(phoneNumber);

                        if ((thisEventTime - lastSentTime).TotalSeconds < 60)
                        {
                            thisEventTime = lastSentTime.AddSeconds(60);
                        }
                    }
                }
                else if (priority == 0)
                {
                    //phoneNumber exists
                    if (lastSentTimeToNumberPriorityOne.ContainsKey(phoneNumber) || lastSentTimeToNumberPriorityZero.ContainsKey(phoneNumber))
                    {
                        DateTime lastSentTime = new DateTime();
                        if (lastSentTimeToNumberPriorityOne.ContainsKey(phoneNumber) && lastSentTimeToNumberPriorityZero.ContainsKey(phoneNumber))
                        {
                            var times = sentTimesForPhoneNumber[phoneNumber];
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
                        else if (!lastSentTimeToNumberPriorityZero.ContainsKey(phoneNumber) && lastSentTimeToNumberPriorityOne.ContainsKey(phoneNumber))
                        {
                            lastSentTime = lastSentTimeToNumberPriorityOne.GetValueOrDefault(phoneNumber);
                        }
                        else if (lastSentTimeToNumberPriorityZero.ContainsKey(phoneNumber) && !lastSentTimeToNumberPriorityOne.ContainsKey(phoneNumber))
                        {
                            lastSentTime = lastSentTimeToNumberPriorityZero.GetValueOrDefault(phoneNumber);
                        }

                        if ((thisEventTime - lastSentTime).TotalSeconds < 60)
                        {
                            thisEventTime = lastSentTime.AddSeconds(60);
                        }
                    }
                }
                
                thisEventTime = getTimeSlot(thisEventTime);

                if (sentTimesForPhoneNumber.ContainsKey(phoneNumber))
                {
                    sentTimesForPhoneNumber[phoneNumber].Add(thisEventTime);
                }
                else
                {
                    sentTimesForPhoneNumber[phoneNumber] = new List<DateTime>() { thisEventTime };
                }
            
                if (priority == 1)
                {
                    lastSentTimeToNumberPriorityOne[phoneNumber] = thisEventTime;
                }
                else
                {
                    lastSentTimeToNumberPriorityZero[phoneNumber] = thisEventTime;
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
                while (startRange < thisDateTime)
                {
                    if (!phoneNumberForTimeSlot.ContainsKey(startRange))
                    {
                        startRange = startRange.AddMilliseconds(1);
                    }
                    else if (phoneNumberForTimeSlot.ContainsKey(startRange))
                    {
                        thisDateTime = startRange.AddSeconds(10);
                        startRange = thisDateTime;
                    }
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
                else if (phoneNumberForTimeSlot.ContainsKey(temp))
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
