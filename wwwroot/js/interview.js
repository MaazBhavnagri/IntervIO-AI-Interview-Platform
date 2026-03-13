document.addEventListener('DOMContentLoaded', function() {
    // Timer logic
    const timerElement = document.getElementById('interviewTimer');
    if (timerElement) {
        let minutes = parseInt(timerElement.getAttribute('data-minutes')) || 10;
        let timeLeft = minutes * 60;
        
        const timerInterval = setInterval(function() {
            timeLeft--;
            
            let displayMinutes = Math.floor(timeLeft / 60);
            let displaySeconds = timeLeft % 60;
            
            displayMinutes = displayMinutes < 10 ? '0' + displayMinutes : displayMinutes;
            displaySeconds = displaySeconds < 10 ? '0' + displaySeconds : displaySeconds;
            
            timerElement.textContent = `${displayMinutes}:${displaySeconds}`;
            
            if (timeLeft < 60) {
                timerElement.classList.remove('text-dark');
                timerElement.classList.add('text-danger');
            }
            
            if (timeLeft <= 0) {
                clearInterval(timerInterval);
                const form = document.getElementById('interviewForm');
                if (form) {
                    form.submit();
                }
            }
        }, 1000);
    }

    let recognition = null;
    let isRecording = false;
    let currentRecordBtn = null;
    let currentTextarea = null;
    let finalTranscript = "";

    // Initialize Web Speech API if supported
    window.SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (window.SpeechRecognition) {
        recognition = new window.SpeechRecognition();
        recognition.continuous = true;
        recognition.interimResults = true;
        
        recognition.onresult = function(event) {
            let interimTranscript = '';
            for (let i = event.resultIndex; i < event.results.length; ++i) {
                if (event.results[i].isFinal) {
                    finalTranscript += event.results[i][0].transcript + " ";
                } else {
                    interimTranscript += event.results[i][0].transcript;
                }
            }
            if (currentTextarea) {
                currentTextarea.value = finalTranscript + interimTranscript;
            }
        };
        
        recognition.onerror = function(event) {
            console.error("Speech recognition error", event.error);
            if(currentRecordBtn) {
                 currentRecordBtn.nextElementSibling.innerHTML = "Error: " + event.error;
            }
        };
        
        recognition.onend = function() {
            // Only toggle off if we hadn't manually stopped it
            if (isRecording && currentRecordBtn) {
                 currentRecordBtn.click();
            }
        };
    }

    const recordButtons = document.querySelectorAll('.record-btn');
    
    recordButtons.forEach(btn => {
        btn.addEventListener('click', async function() {
            const statusSpan = this.nextElementSibling;
            const textarea = this.closest('.card-body').querySelector('.answer-textarea');
            
            if (!recognition) {
                alert("Speech recognition is not supported in your browser. Please use Google Chrome or Edge.");
                return;
            }

            if (isRecording && currentRecordBtn === this) {
                // Stop recording
                recognition.stop();
                isRecording = false;
                
                this.classList.remove('recording', 'btn-danger');
                this.classList.add('btn-outline-danger');
                this.innerHTML = '🎤 Record Answer';
                statusSpan.innerHTML = 'Audio captured successfully!';
                
                setTimeout(() => {
                    statusSpan.style.display = 'none';
                }, 3000);
            } else if (!isRecording) {
                // Start recording
                currentRecordBtn = this;
                currentTextarea = textarea;
                finalTranscript = textarea.value ? textarea.value + " " : "";
                
                recognition.start();
                isRecording = true;

                this.classList.add('recording', 'btn-danger');
                this.classList.remove('btn-outline-danger');
                this.innerHTML = '⏹ Stop Recording';
                statusSpan.innerHTML = 'Listening... Speak now.';
                statusSpan.style.display = 'inline';
                statusSpan.style.color = '#198754';
            } else {
                alert("Please stop the current recording first.");
            }
        });
    });

    // Form submission processing feedback
    const form = document.getElementById('interviewForm');
    if (form) {
        form.addEventListener('submit', function(e) {
            const btn = this.querySelector('button[type="submit"]');
            if (btn) {
                setTimeout(() => { 
                    btn.innerHTML = 'Evaluating with AI Please Wait...';
                    btn.disabled = true; 
                    btn.classList.add('disabled');
                }, 10);
            }
        });
    }
});
