const app =
{
    SpeechRecognition: window.SpeechRecognition || webkitSpeechRecognition,
    SpeechRecognitionEvent: window.SpeechRecognitionEvent || webkitSpeechRecognitionEvent,

    $console: null,
    webSocket: null,
    speechRecognition: null,
    speechRecognitionIsRunning: false,

    log: function (message) {
        app.$console.append($('<div />').text(message));
    },

    initialize: function () {
        app.$console = $('#console');

        app.log('iRacing-STT-VR starting up.');

        app.webSocket = new WebSocket('ws://localhost:43210/');

        app.webSocket.onopen = function (event) {
            app.log('Websocket connection opened.');
        };

        app.webSocket.onclose = function (event) {
            if (event.wasClean) {
                app.log(`Websocket connection closed cleanly, code: ${event.code}, reason: ${event.reason}`);
            } else {
                app.log('Websocket connection died.');
            }
        };

        app.webSocket.onmessage = function (event) {
            // app.log(`Data received from the server: ${event.data}`);

            if (!app.speechRecognitionIsRunning) {
                app.speechRecognition.start();
            }
        };

        app.webSocket.onerror = function (event) {
            app.log('Websocket error.');
        };

        app.speechRecognition = new app.SpeechRecognition();

        app.speechRecognition.continuous = true;
        app.speechRecognition.interimResults = true;

        app.speechRecognition.onaudiostart = function (event) {
            app.log('speechRecognition.onaudiostart');
        };

        app.speechRecognition.onaudioend = function (event) {
            app.log('speechRecognition.onaudioend');
        };

        app.speechRecognition.onend = function (event) {
            app.log('speechRecognition.onend');

            app.speechRecognitionIsRunning = false;
        };

        app.speechRecognition.onerror = function (event) {
            app.log('speechRecognition.onerror');

            switch (event.error) {
                case 'audio-capture':
                    app.log('Audio capture failed.');
                    break;

                case 'not-allowed':
                    app.log('Access to microphone was not given.');
                    break;
            }
        };

        app.speechRecognition.onnomatch = function (event) {
            app.log('speechRecognition.onnomatch');
        };

        app.speechRecognition.onresult = function (event) {
            app.log('speechRecognition.onresult');

            let interimTranscript = '';
            let finalTranscript = '';

            for (let i = event.resultIndex; i < event.results.length; i++) {

                if (event.results[i].isFinal) {
                    finalTranscript += event.results[i][0].transcript;
                } else {
                    interimTranscript += event.results[i][0].transcript;
                }
            }

            if (interimTranscript != '') {
                message = '0 ' + interimTranscript;
                app.log(`Sending: ${message}`);
                app.webSocket.send(message);
            }

            if (finalTranscript != '') {
                message = '1 ' + finalTranscript;
                app.log(`Sending: ${message}`);
                app.webSocket.send(message);
            }
        };

        app.speechRecognition.onsoundstart = function (event) {
            app.log('speechRecognition.onsoundstart');
        };

        app.speechRecognition.onsoundend = function (event) {
            app.log('speechRecognition.onsoundend');
        };

        app.speechRecognition.onspeechstart = function (event) {
            app.log('speechRecognition.onspeechstart');
        };

        app.speechRecognition.onspeechend = function (event) {
            app.log('speechRecognition.onspeechend');
        };

        app.speechRecognition.onstart = function (event) {
            app.log('speechRecognition.onstart');

            app.speechRecognitionIsRunning = true;
        };

        app.speechRecognition.start();
    }
};

$(document).ready(function () {
    app.initialize();
});
