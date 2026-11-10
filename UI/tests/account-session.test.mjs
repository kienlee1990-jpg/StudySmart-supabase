import assert from 'node:assert/strict'
import test from 'node:test'
import { readFileSync } from 'node:fs'
import vm from 'node:vm'
import { hasAccountSession, requiresAccountMode, safeReturnPath } from '../src/utils/accountSession.js'

const token = exp => `header.${Buffer.from(JSON.stringify({ exp })).toString('base64url')}.signature`

test('missing, expired and malformed sessions are guests; only unexpired tokens show account controls', () => {
  let stored = null
  globalThis.localStorage = { getItem: () => stored }
  assert.equal(hasAccountSession(), false)
  stored = token(Date.now() / 1000 - 1)
  assert.equal(hasAccountSession(), false)
  stored = 'invalid'
  assert.equal(hasAccountSession(), false)
  stored = token(Date.now() / 1000 + 3600)
  assert.equal(hasAccountSession(), true)
  assert.equal(requiresAccountMode('Test'), false)
  assert.equal(requiresAccountMode('ExamRoom'), true)
  assert.equal(requiresAccountMode('Tower'), true)
})

test('guests can open home, lessons and tests; protected routes require login and preserve destination', () => {
  const source = readFileSync(new URL('../src/router/index.js', import.meta.url), 'utf8')
  const guardCode = source.slice(source.indexOf('router.beforeEach('), source.indexOf('\nexport default router'))
  let guard
  let signedIn = false
  const context = vm.createContext({
    router: { beforeEach: callback => { guard = callback } },
    hasAccountSession: () => signedIn, requiresAccountMode,
    getStoredUserPermissions: () => [], getStoredUserProfile: () => null,
    canAccessPermission: () => true
  })
  vm.runInContext(guardCode, context)
  const route = (path, mode, requiresAuth = false) => ({ path, fullPath: path, params: { mode }, meta: { requiresAuth } })
  for (const path of ['/', '/subjects/toan', '/learning/toan/5']) assert.equal(guard(route(path)), true)
  assert.equal(guard(route('/quiz-session/toan/Test/5/15', 'Test')), true)
  for (const to of [route('/ranking', null, true), route('/tower-session/toan/1', null, true), route('/quiz-session/toan/ExamRoom/5/60', 'ExamRoom')]) {
    const next = guard(to)
    assert.equal(next.path, '/login')
    assert.equal(next.query.redirect, to.fullPath)
  }
  signedIn = true
  assert.equal(guard(route('/ranking', null, true)), true)
  assert.equal(guard(route('/quiz-session/toan/ExamRoom/5/60', 'ExamRoom')), true)
})

test('login return paths cannot redirect to another site', () => {
  assert.equal(safeReturnPath('/ranking?subject=Toan'), '/ranking?subject=Toan')
  for (const path of ['//example.com', '/\\example.com', 'https://example.com', '/login', undefined]) assert.equal(safeReturnPath(path), '/')
})
