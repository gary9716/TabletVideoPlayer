const { Client,Server,Message } = require('node-osc');
const _ = require('lodash');
const config = require('./config.json');
const path = require('path');
var clientIPs = config.clientIPs;
var fileList = config.fileList;
var progressIndices;
var numDevicesDone = 0;

function isImage(fileName) {
    let imgExt = [".png",".jpg",".jpeg"];
    for(var ext of imgExt) {
        if(fileName.indexOf(ext) > 0) {
            return true;
        }
    }

    return false;
}

function getURL(fileName) {
    if(isImage(fileName)) {
        return 'http://' + oscServerIP + '/img/' + fileName;
    }
    else {
        return 'http://' + oscServerIP + '/video/' + fileName;
    }
    
}

function sendDownloadCmd(ip, index) {
    let msg = new Message('/download-file');
    let fileName = fileList[index];
    let url = getURL(fileName);
    msg.append(url);
    msg.append(1); //interrupt
    msg.append(1); //overwrite
    msg.append(0); //testplay
    return new Promise((resolve,reject) => {
        setTimeout(() => {
            let client = new Client(ip, 9000);
            client.send(msg, (err) => {
                if (err) {
                    reject(err);
                }
                else {
                    resolve();
                }
                client.close();
            });
        }, Math.floor(Math.random() * Math.floor(3)) * 1000);
        
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
var oscServerPort = 8000;
var oscServerIP = getIP("en0");
if(!oscServerIP) oscServerIP = getIP("en1");
console.log('osc server ip:' + oscServerIP);
var oscServer = new Server(oscServerPort, '0.0.0.0');

oscServer.on('message', function (msg) {
  if(msg) {
    let dataList = msg.toString().split(',');
    let oscAddr = dataList[0];
    let deviceIP = dataList[1];
    let fileName = dataList[2];
    let state = dataList[3];
    if(oscAddr === '/download-done') {
        console.log(deviceIP + ':' + fileName + "," + state);
        let index = _.findIndex(fileList, (name) => {
            return name === fileName;
        });
        let newIndex = index + 1;
        if(newIndex >= 0 && newIndex < fileList.length)
            sendDownloadCmd(deviceIP, index + 1);
        else {
            console.log(deviceIP + ':download complete');
            
            numDevicesDone += 1;
            if(numDevicesDone === clientIPs.length) process.exit(0);
            
        }

    }
  }
    
});

var clients = clientIPs.map((ip) => {
    return new Client(ip, 9000);
});

let message = new Message('/set-server');
message.append(oscServerIP);
message.append(oscServerPort);
clientIPs.forEach((ip) => {
    let client = new Client(ip, 9000);
    client.send(message, (err) => {
        if (err) {
          console.error(new Error(err));
        }
        client.close();
    });
});

function DownloadListOfFiles() {
    numDevicesDone = 0;
    let promiseList = clientIPs.map((ip, index) => {
        return sendDownloadCmd(ip, index);
    });
    
    Promise.all(promiseList)
    .catch((err) => {
        console.error(new Error(err));
    });
}
DownloadListOfFiles();


