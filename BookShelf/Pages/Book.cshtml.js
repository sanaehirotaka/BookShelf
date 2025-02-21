
class Book {
    #id;
    #page = 0;
    #pages = [];
    #swiper;

    constructor(id) {
        this.#id = id;
        this.#page = window.location.hash ? parseInt(window.location.hash.substring(1)) | 0 : 0;
    }

    async start() {
        const id = this.#id;
        const list = await (await fetch(`/api/bookshelf/list/${id}`)).json();
        const pages = list.files.map(({ name }) => name);
        this.#pages = pages;
        this.#swiper = new Swiper('.swiper', {
            // Optional parameters
            loop: false, // ループを無効にする
            lazy: true, // 遅延読み込みを有効にする
            zoom: true, // ズームを有効化
            hashNavigation: {
                watchState: true,
            },
            navigation: {
                nextEl: ".swiper-button-next",
                prevEl: ".swiper-button-prev",
            },
            pagination: {
                el: ".swiper-pagination",
                clickable: true,
            },
            // 仮想スライド
            virtual: {
                slides: pages,
                renderSlide: function (slide, index) {
                    return `<div data-hash="${index}" class="swiper-slide"><div class="swiper-zoom-container"><img src="/api/bookshelf/page/${id}/${slide}"></div></div>`;
                },
            },
        });
    }
}