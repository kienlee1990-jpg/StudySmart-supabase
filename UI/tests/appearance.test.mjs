import assert from 'node:assert/strict'
import test from 'node:test'
import 'vue'

test('color choice restores, applies immediately, persists, and handles blocked storage', async () => {
  const previousStorage = globalThis.localStorage
  const previousDocument = globalThis.document
  const values = new Map([['hoc-vui-color-choice', 'tieng-viet']])
  globalThis.localStorage = { getItem: key => values.get(key) ?? null, setItem: (key, value) => values.set(key, value) }
  globalThis.document = { documentElement: { dataset: {} } }
  try {
    const { useAppearance } = await import('../src/composables/useAppearance.js?appearance-test')
    const appearance = useAppearance()
    assert.equal(appearance.colorChoice.value, 'tieng-viet')
    assert.equal(document.documentElement.dataset.colorChoice, 'tieng-viet')
    appearance.setColorChoice('tieng-anh')
    assert.equal(document.documentElement.dataset.colorChoice, 'tieng-anh')
    assert.equal(values.get('hoc-vui-color-choice'), 'tieng-anh')
    const reloaded = await import('../src/composables/useAppearance.js?appearance-reload')
    assert.equal(reloaded.useAppearance().colorChoice.value, 'tieng-anh')
    for (const choice of ['emerald', 'wine', 'navy', 'plum']) {
      appearance.setColorChoice(choice)
      assert.equal(appearance.colorChoice.value, choice)
      assert.equal(document.documentElement.dataset.colorChoice, choice)
      assert.equal(values.get('hoc-vui-color-choice'), choice)
    }
    appearance.setColorChoice('invalid-choice')
    assert.equal(appearance.colorChoice.value, 'auto')
    assert.equal(document.documentElement.dataset.colorChoice, 'auto')
    globalThis.localStorage = { getItem() { throw new Error('blocked') }, setItem() { throw new Error('blocked') } }
    const blocked = await import('../src/composables/useAppearance.js?appearance-blocked')
    blocked.useAppearance().setColorChoice('toan')
    assert.equal(blocked.useAppearance().colorChoice.value, 'toan')
    assert.equal(document.documentElement.dataset.colorChoice, 'toan')
  } finally {
    if (previousStorage === undefined) delete globalThis.localStorage
    else globalThis.localStorage = previousStorage
    if (previousDocument === undefined) delete globalThis.document
    else globalThis.document = previousDocument
  }
})
