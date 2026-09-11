"""Regression cases for provenance of the displayed physical parameters."""
from contextlib import redirect_stdout
import hashlib
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from run import run
from visualize_results import ROOT, read_result, default_config_path


class VisualizationProvenanceTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.folder = Path(self.tmp.name)
        self.config_file = self.folder / 'config.json'
        config = json.loads((ROOT / 'config_beispiel.json').read_text(encoding='utf-8'))
        config['batteries'][0]['capacity_kwh'] = 123
        # Deliberately different formatting: the snapshot must match exact hashed bytes.
        self.bytes = json.dumps(config, ensure_ascii=False, indent=4).encode('utf-8') + b'\r\n'
        self.config_file.write_bytes(self.bytes)
        with redirect_stdout(io.StringIO()):
            run(ROOT / 'beispieldaten/synthetisch_3_tage.csv', self.config_file,
                'peak', self.folder / 'result')

    def tearDown(self):
        self.tmp.cleanup()

    def test_snapshot_remains_associated_after_parameters_change(self):
        directory = self.folder / 'result'
        self.assertEqual((directory / 'config_snapshot.json').read_bytes(), self.bytes)
        self.config_file.write_text('{}', encoding='utf-8')
        result = read_result(directory, self.config_file, 'Test', None)
        self.assertEqual(result['config']['batteries'][0]['capacity_kwh'], 123)
        self.assertEqual(result['summary']['config_sha256'], hashlib.sha256(self.bytes).hexdigest())
        self.assertFalse(result['synthetic'])

    def test_unmatched_parameters_cannot_define_percentage_soc(self):
        directory = self.folder / 'result'
        (directory / 'config_snapshot.json').write_text('{}', encoding='utf-8')
        self.config_file.write_text('{}', encoding='utf-8')
        result = read_result(directory, self.config_file, 'Test', None)
        self.assertIsNone(result['config'])
        self.assertEqual(result['config_status'], 'missing')

    def test_nonfinite_result_is_not_plotted(self):
        path = self.folder / 'result' / 'timeseries.csv'
        text = path.read_text(encoding='utf-8')
        text = text.replace(',40.0,0.0,', ',NaN,0.0,', 1)
        path.write_text(text, encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'non-finite'):
            read_result(path.parent, self.config_file, 'Test', None)

    def test_default_configuration_prefers_saved_gui_inputs(self):
        example = self.folder / 'config_beispiel.json'
        example.write_text('{}', encoding='utf-8')
        with patch('visualize_results.ROOT', self.folder):
            self.assertEqual(default_config_path(), example)
            gui = self.folder / 'config_gui.json'
            gui.write_text('{"control":{"peak_target_kw":50}}', encoding='utf-8')
            self.assertEqual(default_config_path(), gui)


if __name__ == '__main__':
    unittest.main()
