# Windows unicode compatibility per this suggestion https://stackoverflow.com/questions/878972/windows-cmd-encoding-change-causes-python-crash/3259271
import sys, os

if sys.platform == "win32":
    class UniStream(object):
        __slots__= ("fileno", "softspace",)

        def __init__(self, fileobject):
            self.fileno = fileobject.fileno()
            self.softspace = False

        def write(self, text):
            os.write(self.fileno, text.encode("utf_8") if isinstance(text, unicode) else text)

    sys.stdout = UniStream(sys.stdout)
    sys.stderr = UniStream(sys.stderr)
