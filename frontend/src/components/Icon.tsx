const PATHS = {
  plus: 'M12 5v14M5 12h14',
  down: 'M12 4v13M6 11.500l6 6 6-6M5 21h14',
  up: 'M12 20V7M6 12.500l6-6 6 6M5 3h14',
  lock: 'M7 11V8a5 5 0 0 1 10 0v3M5.500 11h13v9.500h-13z',
  copy: 'M9 9h11v11H9zM15 9V4H4v11h5',
  close: 'M6 6l12 12M18 6L6 18',
  left: 'M15 5l-7 7 7 7',
  right: 'M9 5l7 7-7 7',
  warning: 'M12 3.500L22 20.500H2zM12 10v5M12 17.600v.300',
  check: 'M4.500 12.500l5 5 10-11',
} as const

export function Icon({ name }: { name: keyof typeof PATHS }) {
  return (
    <svg className="icon" viewBox="0 0 24 24" aria-hidden="true">
      <path d={PATHS[name]} />
    </svg>
  )
}

export function Star() {
  return (
    <svg className="star" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M12 1c.9 6.600 4.400 10.100 11 11-6.600.9-10.100 4.400-11 11-.9-6.600-4.400-10.100-11-11 6.600-.9 10.100-4.400 11-11z" />
    </svg>
  )
}
