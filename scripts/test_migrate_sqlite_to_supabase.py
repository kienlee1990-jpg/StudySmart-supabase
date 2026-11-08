from datetime import datetime, timezone
from decimal import Decimal
import unittest
from migrate_sqlite_to_supabase import convert


class ConversionTests(unittest.TestCase):
    def test_sqlite_flags_become_postgres_booleans(self):
        self.assertIs(convert(0, 'boolean'), False)
        self.assertIs(convert(1, 'boolean'), True)
        with self.assertRaises(ValueError):
            convert(2, 'boolean')

    def test_timestamps_without_zone_are_existing_utc_values(self):
        self.assertEqual(convert('2026-10-26 05:00:00', 'timestamp with time zone'),
                         datetime(2026, 10, 26, 5, tzinfo=timezone.utc))

    def test_offsets_are_normalized(self):
        self.assertEqual(convert('2026-10-26T12:00:00+07:00', 'timestamp with time zone'),
                         datetime(2026, 10, 26, 5, tzinfo=timezone.utc))

    def test_dotnet_fractional_seconds_work_on_python39(self):
        self.assertEqual(convert('2026-10-23 08:43:53.29092', 'timestamp with time zone'),
                         datetime(2026, 10, 23, 8, 43, 53, 290920, tzinfo=timezone.utc))
        self.assertEqual(convert('2026-10-23T08:43:53.1234567Z', 'timestamp with time zone'),
                         datetime(2026, 10, 23, 8, 43, 53, 123456, tzinfo=timezone.utc))

    def test_scores_and_nulls_are_preserved(self):
        self.assertEqual(convert('98.75', 'numeric'), Decimal('98.75'))
        self.assertIsNone(convert(None, 'timestamp with time zone'))
        self.assertEqual(convert('PasswordHash', 'text'), 'PasswordHash')


if __name__ == '__main__':
    unittest.main()
