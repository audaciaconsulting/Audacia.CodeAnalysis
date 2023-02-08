const { customAlphabet } = import('nanoid');

const chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ01234567890';

function create(size = 10) {
  const nanoid = customAlphabet(chars, size);
  return nanoid();
}

module.exports = {
  create
};
