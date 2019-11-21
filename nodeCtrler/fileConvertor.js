var fs = require('fs')
var path = require('path');
const { spawn } = require('child_process');
let dir = process.argv[2];
fs.readdir(dir, (err, files) => {
    if(err) throw err;
    else {
        let pArray = files.map((file) => {
            if(path.extname(file) == '.MTS') {
                file = path.join(dir,file);
                
                let basename = path.basename(file);
                let index = file.indexOf(path.extname(file));
                if(index >= 0) {
                    return new Promise((resolve,reject)=> {
                        let mp4Name = file.substring(0, index) + ".mp4";
                        console.log(mp4Name);
                        let ffmpeg = spawn('ffmpeg', ['-y','-i', file, "-vcodec" ,"libx264" ,"-crf", "30", "-b:v", "1M", "-acodec", "copy", "-s", "1280x720", mp4Name]);
                        ffmpeg.on('close', (code) => {
                            resolve();
                        });
                    });
                }
                else {
                    return Promise.reject();
                }
            }
        });

        Promise.all(pArray).then(() => {
            console.log('done');
        });
    }
});