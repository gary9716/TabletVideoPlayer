const { Client,Server,Message } = require('node-osc');
const _ = require('lodash');
const config = require('./config.json');
var clientIPs = config.clientIPs;
var videoList = config.videoList;
var progressIndices;
var numDevicesDone = 0;
function sendDownloadVideoCmd(client, index) {
    let msg = new Message('/download-video');
    let url = 'http://' + oscServerIP + '/video/' + videoList[progressIndices[index]];
    msg.append(url);
    msg.append(1); //interrupt
    msg.append(1); //overwrite
    msg.append(0); //testplay
    client.send(msg, (err) => {
        if (err) {
            console.error(new Error(err));
        }
    });
}
let getIP = (interfaceName) => {
    var os = require('os');
    var ifaces = os.networkInterfaces();
    var targetIP = null;
    Object.keys(ifaces).forEach(function (ifname) {
        var alias = 0;

        ifaces[ifname].forEach(function (iface) {
            if ('IPv4' !== iface.family || iface.internal !== false) {
                // skip over internal (i.e. 127.0.0.1) and non-ipv4 addresses
                return;
            }
            if(ifname === interfaceName) {
                targetIP = iface.address;
            }
        });
    });

    return targetIP;
}
var oscServerPort = 9000;
var oscServerIP = getIP("en0");
var oscServer = new Server(oscServerPort, '0.0.0.0');

oscServer.on('message', function (msg) {
  if(msg) {
    let dataList = msg.toString().split(',');
    let oscAddr = dataList[0];
    let deviceIP = dataList[1];
    if(oscAddr === '/download-done') {
        let clientIndex = _.findIndex(clientIPs, function(ip) { return ip === deviceIP; });
        if(clientIndex >= 0) {
            progressIndices[clientIndex] += 1;
            if(progressIndices[clientIndex] >= videoList.length) {
                numDevicesDone += 1;
                console.log('device ' + deviceIP + ' done');
                if(numDevicesDone === clients.length) {
                    process.exit();
                }
            }   
            else 
                sendDownloadVideoCmd(clients[clientIndex], clientIndex);
        }
        else
            console.log('failed to find client');
    }
  }
    
});

var clients = clientIPs.map((ip) => {
    return new Client(ip, 9000);
});

let message = new Message('/set-server');
message.append(oscServerIP);
message.append(oscServerPort);
clients.forEach((client) => {
    client.send(message, (err) => {
        if (err) {
          console.error(new Error(err));
        }
    });
});

function DownloadListOfVideos() {
    numDevicesDone = 0;
    progressIndices = [];
    clients.forEach((client, index) => {
        progressIndices.push(0);
        sendDownloadVideoCmd(client, index);
    });
}

DownloadListOfVideos();
