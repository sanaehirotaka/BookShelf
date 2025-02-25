// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

class Dialog {
    constructor(message, options) {
        this.message = message;
        this.options = {
            dialog: "#confirm", // ダイアログのセレクター
            buttons: ["OK", "キャンセル"], // ボタンのラベル配列
            default: "キャンセル",   // デフォルトで選択されるボタン (またはnull)
            ...options // ユーザー定義のオプションで上書き
        };
        this.dialogElement = document.querySelector(this.options.dialog);
        this.contentElement = this.dialogElement.querySelector(".content");
        this.buttonContainer = this.dialogElement.querySelector(".buttons");
        this.result = null;  // 結果を保持する変数
        this.promiseResolve = null; // Promiseのresolve関数を保持
    }

    async show() {
        // メッセージ設定
        this.contentElement.textContent = this.message;

        // ボタンの生成とイベントリスナーの設定
        this.buttonContainer.innerHTML = ''; // 既存のボタンをクリア
        this.options.buttons.forEach(buttonText => {
            const button = document.createElement("button");
            button.textContent = buttonText;
            button.addEventListener("click", () => this.close(buttonText));
            this.buttonContainer.appendChild(button);

            // デフォルトボタンにフォーカス
            if (this.options.default === buttonText) {
                button.focus();
            }
        });

        // ダイアログ表示 (Promiseを使って結果を待つ)
        return new Promise(resolve => {
            this.promiseResolve = resolve; // resolve関数を保存
            this.dialogElement.showModal();

            // バックドロップクリック時の処理 (キャンセル扱い)
            this.dialogElement.addEventListener("click", (event) => {
                if (event.target === this.dialogElement) {
                    this.close(this.options.default || null); //defaultがnullならnullを返す。
                }
            });

        });
    }


    close(value) {
        this.result = value;
        this.dialogElement.close();
        this.promiseResolve(this.result); // 保存しておいたresolve関数を実行
    }
}

