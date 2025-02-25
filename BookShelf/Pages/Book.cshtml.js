
class Book {
    #id;
    #swiper;

    constructor(id) {
        this.#id = id;
    }

    async startSwiper() {
        const id = this.#id;
        const list = await (await fetch(`/api/bookshelf/list/${id}`)).json();
        const pages = list.files.map(({ name }) => name);
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

    async setup() {

        document.querySelector(".delete").addEventListener("click", async e => {
            await new Dialog("このファイルを削除しますか？").show();
        });
        document.querySelector(".move").addEventListener("click", async e => {
            await new Dialog("このファイルを移動しますか？").show();
        });
    }
}