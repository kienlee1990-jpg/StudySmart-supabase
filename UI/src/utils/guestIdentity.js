const GUEST_ID_KEY = 'hoc-vui-guest-id'

export const getGuestId = () => {
  let guestId = localStorage.getItem(GUEST_ID_KEY)
  if (!guestId) {
    guestId = globalThis.crypto?.randomUUID?.() || 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, char => {
      const value = Math.floor(Math.random() * 16)
      return (char === 'x' ? value : (value & 0x3) | 0x8).toString(16)
    })
    localStorage.setItem(GUEST_ID_KEY, guestId)
  }
  return guestId
}

export const guestRequestConfig = () => ({
  headers: { 'X-Guest-Id': getGuestId() }
})
