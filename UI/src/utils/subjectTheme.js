const subjectSlugs = {
  'Toán': 'toan',
  'Tiếng Anh': 'tieng-anh',
  'Tiếng Việt': 'tieng-viet',
  toan: 'toan',
  'tieng-anh': 'tieng-anh',
  'tieng-viet': 'tieng-viet'
}

export const getSubjectTheme = subject => subjectSlugs[subject] || 'general'
