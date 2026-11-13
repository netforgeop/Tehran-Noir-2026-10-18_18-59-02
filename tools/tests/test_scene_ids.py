import pathlib
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
import scenekit


class SceneIdsTest(unittest.TestCase):
    def test_allocator_skips_existing_ids(self):
        docs = [{"id": "800000000"}, {"id": "800000020"},
                {"id": "9223372036854775807"}]
        builder = scenekit.Builder(docs, {}, "0", "0", next_id=800000000)
        self.assertEqual(builder.take(3), ["800000010", "800000030", "800000040"])

    def test_checker_rejects_overflow_anchor_and_reference(self):
        source = pathlib.Path('Assets/_Project/Scenes/Hilton.unity').read_text()
        bad = source.replace('800000000', '9223372036854776807')
        with tempfile.TemporaryDirectory() as directory:
            path = pathlib.Path(directory) / 'bad.unity'
            path.write_text(bad)
            run = subprocess.run([sys.executable, 'tools/scenecheck.py', str(path)],
                                 text=True, capture_output=True)
        self.assertNotEqual(run.returncode, 0)
        self.assertIn('fileID outside signed 64-bit range', run.stdout)

if __name__ == '__main__':
    unittest.main()
