"use strict";

export {Semaphore};

/* Limits the number of threads that can access a resource or pool of resources concurrently. */
class Semaphore {
    constructor(concurrency = 1) {
        this.concurrency = concurrency;
        this.active = 0;
        this.queue = [];
    }

    /* Asynchronously waits to enter the Semaphore */
    async wait() {
        if (this.active < this.concurrency) {
            this.active++;
            return Promise.resolve();
        }

        return new Promise(resolve => {
            this.queue.push(resolve);
        });
    }

    /* Releases the Semaphore object */
    release() {
        if (this.active > 0) {
            this.active--;
        }

        if (this.queue.length > 0 && this.active < this.concurrency) {
            this.active++;
            const next = this.queue.shift();
            next();
        }
    }
}
