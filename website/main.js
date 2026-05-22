document.addEventListener('DOMContentLoaded', () => {
    // 1. Typewriter Effect
    const typewriterElement = document.getElementById('typewriter');
    const textToType = "ledger --summary --format ascii";
    let index = 0;
    
    // Add an initial delay to simulate loading
    setTimeout(() => {
        typeText();
    }, 1000);

    function typeText() {
        if (index < textToType.length) {
            typewriterElement.textContent += textToType.charAt(index);
            index++;
            // Randomize typing speed for realism
            const typingDelay = Math.random() * 50 + 50; 
            setTimeout(typeText, typingDelay);
        } else {
            // Typing finished, maybe add a blinker class or handle next step
            document.querySelector('.cursor').style.animation = "blink 1s step-end infinite";
        }
    }

    // 2. Intersection Observer for fade-in elements
    const fadeElements = document.querySelectorAll('.fade-in');
    
    const observerOptions = {
        root: null,
        rootMargin: '0px',
        threshold: 0.1
    };

    const observer = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
                // Optional: unobserve if you only want it to animate once
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    fadeElements.forEach(element => {
        observer.observe(element);
    });

    // 3. Interactive Glitch/Scramble Effect on Hover for ASCII Rows
    const asciiRows = document.querySelectorAll('.ascii-row span.cyan, .ascii-row span.accent, .ascii-row span.red');
    const letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789$#@%*";

    asciiRows.forEach(el => {
        // Store original text
        el.dataset.value = el.textContent;
        
        el.addEventListener('mouseover', event => {
            let iterations = 0;
            const originalText = event.target.dataset.value;
            
            clearInterval(el.interval);
            
            el.interval = setInterval(() => {
                event.target.textContent = originalText.split("")
                    .map((letter, i) => {
                        if(i < iterations) {
                            return originalText[i];
                        }
                        return letters[Math.floor(Math.random() * 41)];
                    })
                    .join("");
                
                if(iterations >= originalText.length){
                    clearInterval(el.interval);
                }
                
                iterations += 1 / 3;
            }, 30);
        });
    });
});
