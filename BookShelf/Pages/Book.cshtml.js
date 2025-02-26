
class Book {
    #id;
    #list;
    #swiper;

    constructor(id) {
        this.#id = id;
    }

    async startSwiper() {
        const id = this.#id;
        this.#list = await (await fetch(`/api/bookshelf/list/${id}`)).json();
        const pages = this.#list.files.map(({ name }) => name);
        this.#swiper = new Swiper('.swiper', {
            // Optional parameters
            loop: false, // ループを無効にする
            lazy: true, // 遅延読み込みを有効にする
            zoom: true, // ズームを有効化
            navigation: {
                nextEl: ".swiper-button-next",
                prevEl: ".swiper-button-prev",
            },
            pagination: {
                el: ".swiper-pagination",
                type: "progressbar",
                clickable: true,
            },
            keyboard: {
                enabled: true,
            },
            mousewheel: {
                eventsTarget: ".swiper",
            },
            // 仮想スライド
            virtual: {
                slides: pages,
                renderSlide (slide, index) {
                    return `<div data-hash="${index}" class="swiper-slide">
                        <div class="swiper-zoom-container">
                            <img src="/api/bookshelf/page/${id}/${slide}" loading="lazy">
                        </div>
                    </div>`;
                },
            },
        });
    }

    async setup(shelfs) {
        document.querySelector(".delete").addEventListener("click", async e => {
            const ret = await new Dialog(`[${this.#list.name}]を削除しますか？`).show();
            if (ret == "OK") {
                await fetch(`/api/bookshelf/delete`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        id: this.#id
                    })
                });
                document.location.href = `/`;
            }
        });
        document.querySelector(".move").addEventListener("click", async e => {
            const ret = await new Dialog(`[${this.#list.name}]の移動先を選択してください`, {
                "buttons": [...shelfs.filter(s => s != this.#list.shelf), "キャンセル"]
            }).show();
            if (ret != "キャンセル") {
                await fetch(`/api/bookshelf/move`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        id: this.#id,
                        after: ret
                    })
                });
                document.location.href = `/`;
            }
        });
    }
}