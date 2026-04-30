mergeInto(LibraryManager.library, {

  QuizSpeech_Init: function () {
    if (typeof window === "undefined" || !window.speechSynthesis) return 0;

    var QM = {
      queue: [],
      busy: false,
      voice: null,

      rankVoice: function (v) {
        var n = (v.name || "").toLowerCase();
        var lang = (v.lang || "").toLowerCase();
        var s = 0;
        if (lang.indexOf("en-in") >= 0 || lang === "en-in") s += 55;
        else if (lang.indexOf("hi-in") >= 0) s += 35;
        else if (lang.indexOf("en-gb") >= 0) s += 28;
        else if (lang.indexOf("en-us") >= 0) s += 22;
        else if (lang.indexOf("en") >= 0) s += 12;
        if (n.indexOf("male") >= 0) s += 28;
        if (n.indexOf("daniel") >= 0 || n.indexOf("david") >= 0 || n.indexOf("arthur") >= 0 ||
            n.indexOf("fred") >= 0 || n.indexOf("james") >= 0 || n.indexOf("google uk english male") >= 0) s += 22;
        if (n.indexOf("female") >= 0 || n.indexOf("zira") >= 0 || n.indexOf("samantha") >= 0 ||
            n.indexOf("karen") >= 0 || n.indexOf("victoria") >= 0) s -= 30;
        return s;
      },

      pickVoice: function () {
        var voices = speechSynthesis.getVoices();
        if (!voices || !voices.length) return null;
        var best = voices[0];
        var bestScore = QM.rankVoice(best);
        for (var i = 1; i < voices.length; i++) {
          var sc = QM.rankVoice(voices[i]);
          if (sc > bestScore) {
            bestScore = sc;
            best = voices[i];
          }
        }
        return best;
      },

      refreshVoice: function () {
        QM.voice = QM.pickVoice();
      },

      pump: function () {
        if (QM.busy || QM.queue.length === 0) return;
        QM.busy = true;
        var item = QM.queue.shift();
        var u = new SpeechSynthesisUtterance(item.text);
        if (QM.voice) u.voice = QM.voice;
        u.lang = "en-IN";
        var mood = item.mood | 0;
        if (mood === 1) {
          u.rate = 1.14;
          u.pitch = 1.1;
        } else if (mood === 2) {
          u.rate = 0.94;
          u.pitch = 0.9;
        } else {
          u.rate = 1.05;
          u.pitch = 0.98;
        }
        u.onend = function () {
          QM.busy = false;
          QM.pump();
        };
        u.onerror = function () {
          QM.busy = false;
          QM.pump();
        };
        try {
          speechSynthesis.speak(u);
        } catch (e) {
          QM.busy = false;
          QM.pump();
        }
      },

      enqueue: function (text, mood) {
        if (!text) return;
        QM.queue.push({ text: text, mood: mood | 0 });
        QM.pump();
      }
    };

    window._QuizMasterSpeech = QM;

    QM.refreshVoice();
    if (typeof speechSynthesis.onvoiceschanged !== "undefined") {
      speechSynthesis.onvoiceschanged = function () {
        QM.refreshVoice();
      };
    }

    return 1;
  },

  QuizSpeech_Cancel: function () {
    if (typeof window === "undefined" || !window.speechSynthesis) return;
    try {
      speechSynthesis.cancel();
    } catch (e) {}
    if (window._QuizMasterSpeech) {
      window._QuizMasterSpeech.queue = [];
      window._QuizMasterSpeech.busy = false;
    }
  },

  QuizSpeech_Enqueue: function (strPtr, mood) {
    if (typeof window === "undefined" || !window.speechSynthesis) return;
    var text = UTF8ToString(strPtr);
    if (!text || !text.length) return;
    if (!window._QuizMasterSpeech) return;
    window._QuizMasterSpeech.enqueue(text, mood | 0);
  },

  QuizSpeech_IsBusy: function () {
    if (!window._QuizMasterSpeech) return 0;
    var QM = window._QuizMasterSpeech;
    return (QM.queue.length > 0 || QM.busy) ? 1 : 0;
  }

});
