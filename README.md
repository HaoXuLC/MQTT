# MQTTnetClient

   public MqttNet(string _userName,string _passWord,string _MqttServerIP) 需要账号密码，mqtt服务ip

   MqttServerClient方法使用-服务端使用的mqtt的函数，再收到应答后会进行关闭客户端的操作，主要流程就是连接，发送，关闭连接 ，返回收到的值
   MqttClient客户端使用的mqtt的函数，长连接的处理，适用于设备端进行应答反馈，不带发送的主题
   MqttDisconnect 就是手动关闭客户端
   MqttPublic 与 MqttClient 结合使用  收到信息后进行发布消息
   
  下面是使用例子：  
外部调用的时候：
//#topic是主题
  MqttClient.MqttClient(#topic, listen.Process, (e) => Logger.LogDebug(e, true));

listen.process使用的函数：

   public class MQTT_listen
    {
        private MqttNet mqttNetClient;
        private string topic;
        private ILoggerManager logger;
        private Upload_Main Upload;
        public MQTT_listen(MqttNet _mqttNet,string _topic, Upload_Main _upload,ILoggerManager _logger) {
            mqttNetClient=_mqttNet;
            logger = _logger;
            topic = _topic;
            Upload = _upload;
        }

        public delegate Task<bool> StartUploadHandler(string message, UploadConfigLive uploadConfigLive); //事件委托,输出状态
       public event StartUploadHandler StartProcess; //发布事件

        //收到信息后开始订阅处理 数据 进行发送
        public async Task  Process(MqttApplicationMessageReceivedEventArgs e)
        {
            var json=JsonConvert.DeserializeObject<MQTTObject>(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
            UploadConfigLive method ;
           // var handelFunc=new StartUploadHandler()
            switch (json.method)
            {
                case "StartLive":
                    method = UploadConfigLive.startLive;
                    break;
                case "ControlPTZ":
                    method = UploadConfigLive.controlPTZ;
                    break;
                default:
                    method = UploadConfigLive.closeLive;
                    break;
            }
            var StartProcessStatus = Upload.UploaderLiveProcess(json.value, method).Result;
            ResponseModel(StartProcessStatus);
        }

        private async void  ResponseModel(bool Status)
        {
            var msg = Status ? "ok" : "error";
           await mqttNetClient.MqttPublic(msg, topic, mqttNetClient.mqttClientProp, (e) => logger.LogDebug(e, true));//发布信息
        }
    }
}
