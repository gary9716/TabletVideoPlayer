const fs = require('fs');
const path = require('path');
const videoFiles = [];
const baseNames = [];
const dirName = '0510';
fs.readdir(process.argv[2], (err, files) => {
    if(!err) {
        for(let file of files) {
            if(path.extname(file) === ".mp4") {
                videoFiles.push(dirName + '/' + file);
                baseNames.push(file);
            }
        }

        var config = {};
        config['fileList'] = videoFiles;
        config['basenames'] = baseNames;
        fs.writeFile('new-config.json', JSON.stringify(config), (err) => {
            if (err)
                console.log(err);
            else
                console.log('Write operation complete.');
        });
    }
    else {
        console.log(err);
    }
})
