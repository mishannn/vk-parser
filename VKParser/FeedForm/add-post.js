var newsElement = document.getElementById('news');
if (newsElement) {
    var cardElement = document.createElement('div');
    cardElement.classList.add('card');

    var cardHeader = document.createElement('div');
    cardHeader.classList.add('card-header');
    cardHeader.innerText = '%POST_TITLE%';
    cardElement.appendChild(cardHeader);

    var cardBody = document.createElement('div');
    cardBody.classList.add('card-body');
    cardElement.appendChild(cardBody);

    var postText = document.createElement('p');
    postText.innerText = '%POST_TEXT%';
    cardBody.appendChild(postText);

    newsElement.appendChild(cardElement);
}