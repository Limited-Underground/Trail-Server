import type { Metadata } from 'next';
import { Geist, Geist_Mono } from 'next/font/google';
import './globals.css';

const geistSans = Geist({ variable: '--font-geist-sans', subsets: ['latin'] });
const geistMono = Geist_Mono({ variable: '--font-geist-mono', subsets: ['latin'] });

export const metadata: Metadata = {
  title: 'Limited Underground Trail Server — Public Prototype',
  description: 'Non-operational public interface prototype and architecture foundation for the Limited Underground Trail Server.',
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body className={geistSans.variable + ' ' + geistMono.variable}>{children}</body></html>;
}
