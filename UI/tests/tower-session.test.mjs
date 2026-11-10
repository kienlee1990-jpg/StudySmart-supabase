import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'
import vm from 'node:vm'
import { computed, ref } from 'vue'

const source = readFileSync(new URL('../src/components/quiz/QuizPlay.vue', import.meta.url), 'utf8')
const script = source.match(/<script setup>([\s\S]*?)<\/script>/)[1].replace(/^import .*$/gm, '')

function session(post) {
  let now = 0
  let tick
  const requests = []
  const context = vm.createContext({
    computed, ref, onMounted() {}, onUnmounted() {},
    defineProps: () => ({}), useRouter: () => ({ replace() {} }),
    localStorage: { getItem: () => null }, sessionStorage: { setItem() {} },
    guestRequestConfig: () => ({}),
    httpClient: { post: async (url, data) => {
      requests.push({ url, data })
      return post ? post(url, data) : { data: { isCorrect: data.optionId === 11, correctOptionId: 11, correctOption: 'A' } }
    } },
    Date: { now: () => now },
    setInterval: callback => { tick = callback; return 1 }, clearInterval: () => { tick = null }
  })
  vm.runInContext(`${script}
    quiz.value = { mode: 'Tower', questions: [{ id: 1 }, { id: 2 }] };
    attempt.value = { id: 99 };
    globalThis.state = { answered, feedback, secondsLeft, towerLocked, towerTimedOut, towerAnswerError,
      activeQuestionIndex, goToQuestion, startTowerCountdown, chooseActiveAnswer, gradeTowerAnswer, submit };
  `, context)
  return { ...context.state, requests, advance: ms => { now += ms; tick?.() }, elapseWithoutTick: ms => { now += ms } }
}
const flush = () => new Promise(resolve => setImmediate(resolve))

test('15-second timeout locks an unanswered question, reveals feedback, and only permits moving forward', async () => {
  const game = session()
  game.startTowerCountdown()
  assert.equal(game.secondsLeft.value, 15)
  game.goToQuestion(1)
  assert.equal(game.activeQuestionIndex.value, 0)
  game.advance(14_999)
  assert.equal(game.secondsLeft.value, 1)
  assert.equal(game.requests.length, 0)
  game.advance(1)
  await flush()
  assert.equal(game.secondsLeft.value, 0)
  assert.equal(game.towerTimedOut.value, true)
  assert.equal(game.requests[0].data.optionId, null)
  assert.equal(game.feedback.value[1].isCorrect, false)
  assert.equal(game.feedback.value[1].correctOptionId, 11)
  game.chooseActiveAnswer(11)
  assert.equal(game.requests.length, 1)
  game.goToQuestion(1)
  assert.equal(game.secondsLeft.value, 15)
  game.goToQuestion(0)
  assert.equal(game.activeQuestionIndex.value, 1)
})

test('selection is graded immediately and cannot change while grading or after feedback', async () => {
  let resolve
  const game = session(() => new Promise(done => { resolve = done }))
  game.startTowerCountdown()
  game.chooseActiveAnswer(11)
  game.chooseActiveAnswer(12)
  game.goToQuestion(1)
  assert.equal(game.towerLocked.value, true)
  assert.equal(game.answered.value[1], 11)
  assert.equal(game.requests.length, 1)
  assert.equal(game.activeQuestionIndex.value, 0)
  resolve({ data: { isCorrect: true, correctOptionId: 11, correctOption: 'A' } })
  await flush()
  assert.equal(game.feedback.value[1].isCorrect, true)
  game.chooseActiveAnswer(12)
  assert.equal(game.requests.length, 1)
})

test('a delayed timer cannot accept a selection after the deadline', async () => {
  const game = session()
  game.startTowerCountdown()
  game.elapseWithoutTick(15_001)
  game.chooseActiveAnswer(11)
  await flush()
  assert.equal(game.requests[0].data.optionId, null)
  assert.equal(game.towerTimedOut.value, true)
})

test('failed grading retries the locked choice and prevents premature submission', async () => {
  let fail = true
  const game = session(async () => {
    if (fail) throw new Error('offline')
    return { data: { isCorrect: false, correctOptionId: 11, correctOption: 'A' } }
  })
  game.startTowerCountdown()
  game.chooseActiveAnswer(12)
  await flush()
  assert.ok(game.towerAnswerError.value)
  game.chooseActiveAnswer(11)
  game.goToQuestion(1)
  await game.submit()
  assert.equal(game.requests.length, 1)
  assert.equal(game.activeQuestionIndex.value, 0)
  fail = false
  await game.gradeTowerAnswer()
  assert.equal(game.requests[1].data.optionId, 12)
  assert.equal(game.feedback.value[1].isCorrect, false)
})
