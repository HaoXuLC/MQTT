using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnetClient
{
    public class MqttNet
    {
        private string userName;
        private string password;
        private string MqttServerIP;
        public bool ConnectStatus { get; set; }

        public IMqttClient mqttClientProp { get; set; }
        /// <summary>
        /// 公司内部使用的接口
        /// </summary>
        /// <param name="_userName"></param>
        /// <param name="_passWord"></param>
        /// <param name="_MqttServerIP"></param>
        public MqttNet(string _userName,string _passWord,string _MqttServerIP)
        {
            userName = _userName;
            password = _passWord;
            MqttServerIP = _MqttServerIP;
        }
        public MqttNet()
        {

        }

        public delegate Task SubscribeProcess(MqttApplicationMessageReceivedEventArgs e);//委托事件声明，可以传参外部的方法
        public delegate void LoggerProcess(string message); //用来存储日志的委托
        /// <summary>
        /// 服务端使用的mqtt的函数，再收到应答后会进行关闭客户端的操作，主要流程就是连接，发送，关闭连接 ，返回收到的值
        /// </summary>
        /// <param name="message"></param>
        /// <param name="snTopic"></param>
        /// <returns></returns>
        public async Task<string> MqttServerClient(string message,string snTopic,LoggerProcess loggerProcess)
        {
            var mqttFactory = new MqttFactory();
            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                await MqttConnect(mqttClient,loggerProcess);
                await MqttPublic(message, snTopic, mqttClient, loggerProcess);
                var responseString= await Handle_Received_Application_Message(mqttFactory, mqttClient, snTopic,loggerProcess);

               await mqttClient.DisconnectAsync();

                return responseString;
            }
        }
        /// <summary>
        /// 客户端使用的mqtt的函数，长连接的处理，适用于设备端进行应答反馈，不带发送的主题
        /// </summary>
        /// <param name="message"></param>
        /// <param name="snTopic"></param>
        /// <param name="eventProcess"></param>
        /// <returns></returns>

        public async Task MqttClient(string snTopic, SubscribeProcess eventProcess, LoggerProcess loggerProcess)
        {
            var mqttFactory = new MqttFactory();
            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                await MqttConnect(mqttClient, loggerProcess);
                await Handle_Received_Application_MessageLong(mqttFactory, mqttClient, snTopic,eventProcess,loggerProcess);
            }
        }
       /// <summary>
       /// 这里公开带有发送的功能函数
       /// </summary>
       /// <param name="message"></param>
       /// <param name="snTopic"></param>
       /// <param name="mqttClient"></param>
       /// <returns></returns>
        public async Task MqttPublic(string message, string snTopic, IMqttClient mqttClient, LoggerProcess loggerProcess)
        {
            var applicationMessage = new MqttApplicationMessageBuilder() //构建消息信息
               .WithTopic(snTopic)
               .WithPayload(message).Build();

            await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);//发布topic的值给server

             loggerProcess($"Mqtt发送数据:{message},topic:{snTopic}");
        }
        /// <summary>
        /// 这里私有进行mqtt的连接
        /// </summary>
        /// <param name="mqttClient"></param>
        /// <returns></returns>
        private async Task MqttConnect(IMqttClient mqttClient, LoggerProcess loggerProcess)
        {
            var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(MqttServerIP)
               .WithCredentials(userName, password)
               .WithProtocolVersion(MqttProtocolVersion.V500).Build();
            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            //ConnectStatus=mqttClient.IsConnected;
            mqttClientProp = mqttClient;
            //这里存log
            loggerProcess("Mqtt连接成功!");
        }

        /// <summary>
        /// 外部关闭mqtt的连接
        /// </summary>
        /// <param name="mqttClient"></param>
        /// <param name="loggerProcess"></param>
        /// <returns></returns>
        public async Task MqttDisconnect(IMqttClient mqttClient, LoggerProcess loggerProcess)
        {
            await mqttClient.DisconnectAsync();
            //ConnectStatus = mqttClient.IsConnected;

            loggerProcess("Mqtt关闭成功!");

        }

        /// <summary>
        /// 订阅接受mqtt对端的值
        /// </summary>v
        /// <param name="mqttFactory"></param>
        /// <param name="mqttClient"></param>
        /// <returns></returns>
        private async Task<string> Handle_Received_Application_Message(MqttFactory mqttFactory, IMqttClient mqttClient,string snTopic, LoggerProcess loggerProcess)
        {
            var tcs = new TaskCompletionSource<string>();
            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                tcs.SetResult(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
                return Task.CompletedTask;
            };

            await Handle_Subscribe(mqttFactory, mqttClient, snTopic);

            //这里存log
             loggerProcess("Mqtt完成订阅等待消息发送!");

            return tcs.Task.Result;
        }
        /// <summary>
        /// 长连接使用的接受订阅的消息,带外部委托的消息
        /// </summary>
        /// <param name="mqttFactory"></param>
        /// <param name="mqttClient"></param>
        /// <param name="snTopic"></param>
        /// <param name="eventProcess"></param>
        /// <returns></returns>
        private async Task Handle_Received_Application_MessageLong(MqttFactory mqttFactory, IMqttClient mqttClient,string snTopic,SubscribeProcess eventProcess, LoggerProcess loggerProcess)
        {
            mqttClient.ApplicationMessageReceivedAsync += e => eventProcess(e);

            await Handle_Subscribe(mqttFactory, mqttClient, snTopic);

            //这里存log
             loggerProcess("Mqtt完成订阅等待消息发送!");



            Console.ReadLine();
        }
        /// <summary>
        /// 相关主题订阅的方式
        /// </summary>
        /// <param name="mqttFactory"></param>
        /// <param name="mqttClient"></param>
        /// <param name="snTopic"></param>
        /// <returns></returns>
        private async Task Handle_Subscribe(MqttFactory mqttFactory, IMqttClient mqttClient, string snTopic)
        {
          var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
         .WithTopicFilter(
             f =>
             {
                 f.WithTopic(snTopic);
             })
                .Build();

                await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
            }

    }
}